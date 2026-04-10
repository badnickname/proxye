using System.Net.Sockets;
using Proxye.Core.Implementations.Channel;
using Proxye.Core.Models;

namespace Proxye.Core.Implementations;

public class OutChannelFactory
{
    public IChannel CreateDirect(Host host, Memory<byte> buffer)
    {
        return new DirectOutChannel(host, buffer);
    }

    public IChannel CreateProxied(Host host, Memory<byte> buffer)
    {
        return new ProxyOutChannel(host, buffer);
    }
}