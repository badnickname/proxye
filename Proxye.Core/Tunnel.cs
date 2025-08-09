using System.Buffers;
using System.Net.Sockets;
using System.Reflection;
using Proxye.Core.Helpers;

namespace Proxye.Core;

public sealed class Tunnel : IDisposable
{
    private readonly ProxyeOptions _options;
    private readonly byte[] _localBuffer;
    private readonly byte[] _remoteBuffer;
    private readonly Socket _socket;
    private static readonly byte[] HostArray = "Host: ".ToArray().Select(x => (byte) x).ToArray();
    private static readonly int HostHash = HostArray.Select(x => (int) x).Sum();
    private static readonly byte[] ConnectArray = "CONNECT ".ToArray().Select(x => (byte) x).ToArray();
    private static readonly int ConnectHash = ConnectArray.Select(x => (int) x).Sum();
    private static readonly byte[] Established = $"HTTP/1.1 200 Connection Established\r\nProxy-Agent: Proxye/{Assembly.GetExecutingAssembly().GetName().Version!.ToString()}\r\n\r\n".ToArray().Select(x => (byte) x).ToArray();
    private CancellationTokenSource? _cancellationTokenSource;

    public string Host { get; private set; }

    public uint Port { get; private set; }

    public Socket? RemoteSocket { get; private set; }

    public Tunnel(Socket socket, ProxyeOptions options)
    {
        _socket = socket;
        _options = options;
        _localBuffer = ArrayPool<byte>.Shared.Rent(65535);
        _remoteBuffer = ArrayPool<byte>.Shared.Rent(65535);
    }

    public async Task StartAsync(CancellationToken token)
    {
        // Get Host and Port of remote server
        var count = await _socket.ReceiveAsync(_localBuffer, token);
        var startOf = StringHelpers.GetStartOf(count, _localBuffer, HostHash, HostArray);
        Host = StringHelpers.Read(_localBuffer.AsSpan()[(startOf + HostArray.Length)..], out var length, ':');
        Port = _localBuffer[startOf + HostArray.Length + length] == ':'
            ? uint.Parse(StringHelpers.Read(_localBuffer.AsSpan()[(startOf + HostArray.Length + length + 1)..], out _))
            : 80;

        var isHttps = StringHelpers.GetStartOf(count, _localBuffer, ConnectHash, ConnectArray) > -1;
        var rule = _options.Match(Host);
        switch (rule?.Protocol)
        {
            case "HTTP":
                // Just send all data to another proxy
                RemoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await RemoteSocket.ConnectAsync(rule.Host, rule.Port, token);
                break;
            default:
                // Send 200 OK to client if it's https request
                if (isHttps)
                {
                    await _socket.SendAsync(Established, token);
                    count = await _socket.ReceiveAsync(_localBuffer, token);
                }
                RemoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await RemoteSocket.ConnectAsync(Host, (int) Port, token);
                break;
        }

        await RemoteSocket.SendAsync(_localBuffer.AsMemory()[..count], token);
    }

    public async Task LoopAsync(CancellationToken token)
    {
        var cs = _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        var remote = Task.Run(async () =>
        {
            while (!cs.IsCancellationRequested && RemoteSocket!.IsConnected())
            {
                var count = await RemoteSocket!.ReceiveAsync(_remoteBuffer, cs.Token);
                if (cs.IsCancellationRequested) return;
                await _socket.SendAsync(_remoteBuffer.AsMemory()[..count], cs.Token);
            }
            await cs.CancelAsync();
        }, cs.Token);
        var local = Task.Run(async () =>
        {
            while (!cs.IsCancellationRequested && _socket.IsConnected())
            {
                var count = await _socket.ReceiveAsync(_localBuffer, cs.Token);
                if (cs.IsCancellationRequested) return;
                await RemoteSocket!.SendAsync(_localBuffer.AsMemory()[..count], cs.Token);
            }
            await cs.CancelAsync();
        }, cs.Token);
        await Task.WhenAny(local, remote);
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_localBuffer);
        ArrayPool<byte>.Shared.Return(_remoteBuffer);
        RemoteSocket?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}