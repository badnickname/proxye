using Proxye.Shared;
using Proxye.Udp;

namespace Proxye.Interfaces;

internal interface IUdpTunnel
{
    Task<TunnelConnection> StartAsync(Memory<byte> received, TunnelUdpContext context);

    ValueTask<int> TunnelLocal(Memory<byte> received, TunnelUdpContext context);
}