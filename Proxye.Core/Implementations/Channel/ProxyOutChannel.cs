using System.Net.Sockets;
using Proxye.Core.Models;

namespace Proxye.Core.Implementations.Channel;

public class ProxyOutChannel(Host proxy, Host host, Memory<byte> buffer) : IChannel
{
    private readonly TcpClient _client = new();
    private NetworkStream _stream = null!;
    private static readonly byte[] Socks5ConnectArray = [5, 1, 0];

    public async Task EstablishAsync(CancellationToken token)
    {
        await _client.ConnectAsync(proxy.Address, proxy.Port, token);
        _stream = _client.GetStream();

        await _stream.WriteAsync(Socks5ConnectArray, token);
        await _stream.ReadAsync(buffer, token); // todo: handle answer

        buffer.Span[0] = 5;
        buffer.Span[1] = 1;
        buffer.Span[2] = 0;
        buffer.Span[3] = 3;
        buffer.Span[4] = (byte) host.Address.Length;

        for (var i = 0; i < host.Address.Length; i++)
            buffer.Span[5 + i] = (byte) host.Address[i];

        buffer.Span[5 + host.Address.Length] = (byte) (host.Port >> 8);
        buffer.Span[6 + host.Address.Length] = (byte) (host.Port & 0xff);

        await _stream.WriteAsync(buffer[..(7 + host.Address.Length)], token);
        await _stream.ReadAsync(buffer, token); // todo: handle answer
    }
    
    public bool IsConnected => _stream is { CanRead: true, CanWrite: true } && _client.Connected;

    public void GetHost(out Host hst)
    {
        hst = host;
    }

    public void Close()
    {
        _stream.Dispose();
        _client.Dispose();
    }

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