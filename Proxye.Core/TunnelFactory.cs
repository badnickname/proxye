using Proxye.Core.Implementations;
using Proxye.Core.Models;

namespace Proxye.Core;

public class TunnelFactory(IRules rules, InChannelFactory inFactory, OutChannelFactory outFactory)
{
    public Tunnel Create() => new(rules, inFactory, outFactory);
}
