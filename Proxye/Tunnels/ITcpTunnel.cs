using Proxye.Models;

namespace Proxye.Tunnels;

public interface ITcpTunnel
{
    Task<TunnelConnection> StartAsync(Memory<byte> received, TunnelTcpContext context);

    ValueTask<int> TunnelLocal(Memory<byte> received, TunnelTcpContext context);

    ValueTask<int> TunnelRemote(Memory<byte> received, TunnelTcpContext context);
}