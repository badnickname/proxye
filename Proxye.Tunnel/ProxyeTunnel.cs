using System.Buffers;
using System.Net.Sockets;
using Proxye.Rules.Helpers;
using Proxye.Tunnel.Models;
using Proxye.Tunnel.Protocols;

namespace Proxye.Tunnel;

public interface IProxyeTunnel : IDisposable
{
    string Host { get; }

    uint Port { get; }

    Task StartAsync(CancellationToken token);

    Task LoopAsync(CancellationToken token);
}

internal sealed class ProxyeTunnel : IProxyeTunnel
{
    private readonly byte[] _localBuffer;
    private readonly byte[] _remoteBuffer;
    private readonly Socket _socket;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly IProtocolFactory _factory;
    private TunnelContext _context;
    private IProtocol? _tunnel;

    public string Host { get; private set; }

    public uint Port { get; private set; }

    public Socket? RemoteSocket { get; private set; }

    public ProxyeTunnel(Socket socket, IProtocolFactory factory)
    {
        _socket = socket;
        _localBuffer = ArrayPool<byte>.Shared.Rent(65535);
        _remoteBuffer = ArrayPool<byte>.Shared.Rent(65535);
        _factory = factory;
    }

    public async Task StartAsync(CancellationToken token)
    {
        var count = await _socket.ReceiveAsync(_localBuffer, token);
        
        if (count > 0 && _localBuffer[0] == 5) // Handle SOCKS5
        {
            _tunnel = _factory.CreateSocks5();
        }
        else // Handle HTTP
        {
            _tunnel = _factory.CreateHttp();
        }

        _context = new TunnelContext
        {
            CancellationToken = token,
            LocalBuffer = _localBuffer,
            RemoteBuffer = _remoteBuffer,
            Socket = _socket
        };
        var response = await _tunnel.HandshakeAsync(_localBuffer.AsMemory()[..count], _context);
        _context.RemoteSocket = response.Socket;
        RemoteSocket = _context.RemoteSocket;
        Host = response.Host;
        Port = response.Port;
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
                await _context.Socket.SendAsync(_remoteBuffer.AsMemory()[..count], _context.CancellationToken);
            }
            await cs.CancelAsync();
        }, cs.Token);
        var local = Task.Run(async () =>
        {
            while (!cs.IsCancellationRequested && _socket.IsConnected())
            {
                var count = await _socket.ReceiveAsync(_localBuffer, cs.Token);
                if (cs.IsCancellationRequested) return;
                await _context.RemoteSocket!.SendAsync(_localBuffer.AsMemory()[..count], _context.CancellationToken);
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