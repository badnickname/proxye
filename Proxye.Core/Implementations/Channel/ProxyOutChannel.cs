using System.Net.Sockets;
using Proxye.Core.Models;

namespace Proxye.Core.Implementations.Channel;

public class ProxyOutChannel(Host proxy, Host host, Memory<byte> buffer) : BaseChannel(buffer)
{
    private readonly TcpClient _client = new();
    private static readonly byte[] Socks5ConnectArray = [5, 1, 0];

    public override async Task EstablishAsync(CancellationToken token)
    {
        await _client.ConnectAsync(proxy.Address, proxy.Port, token);
        TcpClient = _client;
        var stream = _client.GetStream();

        await stream.WriteAsync(Socks5ConnectArray, token);
        await stream.ReadAsync(buffer, token); // todo: handle answer

        buffer.Span[0] = 5;
        buffer.Span[1] = 1;
        buffer.Span[2] = 0;
        buffer.Span[3] = 3;
        buffer.Span[4] = (byte) host.Address.Length;

        for (var i = 0; i < host.Address.Length; i++)
            buffer.Span[5 + i] = (byte) host.Address[i];

        buffer.Span[5 + host.Address.Length] = (byte) (host.Port >> 8);
        buffer.Span[6 + host.Address.Length] = (byte) (host.Port & 0xff);

        await stream.WriteAsync(buffer[..(7 + host.Address.Length)], token);
        await stream.ReadAsync(buffer, token); // todo: handle answer
    }

    protected override Host Host => host;

    public override void Dispose()
    {
        Disconnect();
        _client.Dispose();
    }
}