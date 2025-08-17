using Microsoft.Extensions.DependencyInjection;
using Proxye.Tcp;
using Proxye.Udp;

namespace Proxye.Shared;

internal interface ITunnelFactory
{
    ITcpTunnel CreateSocks5();
    ITcpTunnel CreateHttp();
    IUdpTunnel CreateDns();
}

internal sealed class TunnelFactory(IServiceProvider provider) : ITunnelFactory
{
    public ITcpTunnel CreateSocks5() => provider.GetRequiredService<Socks5Tunnel>();

    public ITcpTunnel CreateHttp() => provider.GetRequiredService<HttpTunnel>();

    public IUdpTunnel CreateDns() => provider.GetRequiredService<DnsTunnel>();
}