using System.Buffers;
using System.Net;
using System.Net.Sockets;
using Proxye.Helpers;
using Proxye.Models;
using Proxye.Tunnels;

namespace Proxye;

internal sealed class ProxyeTcpTunnel : IProxyeTunnel
{
    private readonly byte[] _localBuffer;
    private readonly byte[] _remoteBuffer;
    private readonly Socket _socket;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly ITunnelFactory _factory;
    private ITcpTunnel? _tunnel;
    private TunnelTcpContext _context;

    public string Host { get; private set; }

    public uint Port { get; private set; }

    public Socket? RemoteSocket { get; private set; }

    public ProxyeTcpTunnel(Socket socket, ITunnelFactory factory)
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

        _context = new TunnelTcpContext
        {
            CancellationToken = token,
            LocalBuffer = _localBuffer,
            RemoteBuffer = _remoteBuffer,
            Socket = _socket
        };
        var response = await _tunnel.StartAsync(_localBuffer.AsMemory()[..count], _context);
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
                await _tunnel!.TunnelRemote(_remoteBuffer.AsMemory()[..count], _context);
            }
            await cs.CancelAsync();
        }, cs.Token);
        var local = Task.Run(async () =>
        {
            while (!cs.IsCancellationRequested && _socket.IsConnected())
            {
                var count = await _socket.ReceiveAsync(_localBuffer, cs.Token);
                if (cs.IsCancellationRequested) return;
                await _tunnel!.TunnelLocal(_localBuffer.AsMemory()[..count], _context);
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