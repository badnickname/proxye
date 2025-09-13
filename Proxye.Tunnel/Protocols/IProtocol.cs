using Proxye.Tunnel.Models;

namespace Proxye.Tunnel.Protocols;

internal interface IProtocol
{
    Task<TunnelConnection> HandshakeAsync(Memory<byte> received, TunnelContext context);
}