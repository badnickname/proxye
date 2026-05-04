using System.Net.Sockets;
using Proxye.Core.Models;

namespace Proxye.Core.Implementations.Channel;

public abstract class BaseChannel(Memory<byte> buffer) : IChannel
{
    private NetworkStream _stream = null!;

    protected TcpClient TcpClient
    {
        set => _stream = value.GetStream();
    }

    protected abstract Host Host { get; }

    protected void Disconnect()
    {
        _stream.Dispose();
    }

    public abstract void Dispose();

    public void GetHost(out Host host)
    {
        host = Host;
    }

    public bool IsConnected { get; set; } = true;

    public async Task SendAsync(Memory<byte> bytes, CancellationToken token)
    {
        if (!IsConnected)
            return;

        await _stream.WriteAsync(bytes, token);
    }

    public async Task<Memory<byte>> ReceiveAsync(CancellationToken token)
    {
        var receive = await _stream.ReadAsync(buffer, token);
        if (receive == 0)
            IsConnected = false;

        return buffer[..receive];
    }

    public abstract Task EstablishAsync(CancellationToken token);
}