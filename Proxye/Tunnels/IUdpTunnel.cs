using Proxye.Models;

namespace Proxye.Tunnels;

internal interface IUdpTunnel
{
    Task<TunnelConnection> StartAsync(Memory<byte> received, TunnelUdpContext context);

    ValueTask<int> TunnelLocal(Memory<byte> received, TunnelUdpContext context);
}