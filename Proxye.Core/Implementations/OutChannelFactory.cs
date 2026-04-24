using Proxye.Core.Implementations.Channel;
using Proxye.Core.Models;

namespace Proxye.Core.Implementations;

public class OutChannelFactory(IRules rules)
{
    public IChannel CreateDirect(Host host, Memory<byte> buffer)
        => new DirectOutChannel(host, buffer);

    public IChannel CreateProxied(Host host, Memory<byte> buffer)
        => new ProxyOutChannel(rules.Host, host, buffer);
}
