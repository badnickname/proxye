using System.Net.Sockets;
using Proxye.Core.Models;

namespace Proxye.Core.Implementations.Channel;

public sealed class DirectOutChannel(Host host, Memory<byte> buffer) : BaseChannel(buffer)
{
    private readonly TcpClient _client = new();

    public override async Task EstablishAsync(CancellationToken token)
    {
        await _client.ConnectAsync(host.Address, host.Port, token);
        TcpClient = _client;
    }

    protected override Host Host => host;

    public override void Dispose()
    {
        Disconnect();
        _client.Dispose();
    }
}
