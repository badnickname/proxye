using Proxye.Shared;
using Proxye.Tcp;

namespace Proxye.Interfaces;

public interface ITcpTunnel
{
    Task<TunnelConnection> StartAsync(Memory<byte> received, TunnelTcpContext context);
}