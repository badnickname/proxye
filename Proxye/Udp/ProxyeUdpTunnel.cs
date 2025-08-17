using System.Buffers;
using System.Net.Sockets;
using Proxye.Interfaces;
using Proxye.Shared;

namespace Proxye.Udp;

internal sealed class UdpProxyeTunnel(UdpReceiveResult result, UdpClient client, ITunnelFactory factory) : IProxyeTunnel
{
    private byte[]? _localBuffer;
    private IUdpTunnel? _tunnel;
    private TunnelUdpContext _context;
    private static readonly SemaphoreSlim Semaphore = new(1);

    public async Task StartAsync(CancellationToken token = default)
    {
        _localBuffer = ArrayPool<byte>.Shared.Rent(65535);
        _context = new TunnelUdpContext
        {
            Socket = client,
            ReceiveResult = result,
            CancellationToken = token,
        };
        _tunnel = factory.CreateDns();
        var response = await _tunnel.StartAsync(result.Buffer.AsMemory(), _context);
        _context.RemoteSocket = response.Socket;
        RemoteSocket = _context.RemoteSocket;
        Host = response.Host;
        Port = response.Port;
    }

    public async Task LoopAsync(CancellationToken token = default)
    {
        var count = await RemoteSocket!.ReceiveAsync(_localBuffer, token);
        if (token.IsCancellationRequested) return;
        await Semaphore.WaitAsync(token);
        try
        {
            await _context.Socket.SendAsync(_localBuffer.AsMemory()[..count], _context.ReceiveResult.RemoteEndPoint, _context.CancellationToken);
            // await _tunnel!.TunnelLocal(_localBuffer.AsMemory()[..count], _context);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public string Host { get; private set; }

    public uint Port { get; private set; }

    public Socket? RemoteSocket { get; private set; }

    public void Dispose()
    {
        if (_localBuffer is not null) ArrayPool<byte>.Shared.Return(_localBuffer);
        RemoteSocket?.Dispose();
    }
}