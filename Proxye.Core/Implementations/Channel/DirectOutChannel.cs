using System.Net.Sockets;
using Proxye.Core.Models;

namespace Proxye.Core.Implementations.Channel;

public class DirectOutChannel(Host host, Memory<byte> buffer) : IChannel
{
    private readonly TcpClient _client = new();
    private NetworkStream _stream = null!;

    public async Task EstablishAsync(CancellationToken token)
    {
        await _client.ConnectAsync(host.Address, host.Port, token);
        _stream = _client.GetStream();
    }

    public void GetHost(out Host hst)
    {
        hst = host;
    }

    public bool IsConnected => _stream is { CanRead: true, CanWrite: true } && _client.Connected;

    public async Task SendAsync(Memory<byte> bytes, CancellationToken token)
    {
        if (bytes.Length == 0) return;

        await _stream.WriteAsync(bytes, token);
    }

    public async Task<Memory<byte>> ReceiveAsync(CancellationToken token)
    {
        if (_client.Available < 1) 
        {
            await Task.Delay(100, token);
        }

        var receive = await _stream.ReadAsync(buffer, token);

        return buffer[..receive];
    }

    public void Dispose()
    {
        _stream.Dispose();
        _client.Dispose();
    }
}
