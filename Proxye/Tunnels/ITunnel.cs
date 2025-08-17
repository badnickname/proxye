using Proxye.Models;

namespace Proxye.Tunnels;

public interface ITunnel
{
    Task<TunnelConnection> StartAsync(Memory<byte> received, TunnelContext context);

    ValueTask<int> TunnelLocal(Memory<byte> received, TunnelContext context);

    ValueTask<int> TunnelRemote(Memory<byte> received, TunnelContext context);
}