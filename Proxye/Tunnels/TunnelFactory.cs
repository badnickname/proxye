using Microsoft.Extensions.DependencyInjection;

namespace Proxye.Tunnels;

internal interface ITunnelFactory
{
    ITunnel CreateSocks5();
    ITunnel CreateHttp();
    ITunnel CreateDns();
}

internal sealed class TunnelFactory(IServiceProvider provider) : ITunnelFactory
{
    public ITunnel CreateSocks5() => provider.GetRequiredService<Socks5Tunnel>();

    public ITunnel CreateHttp() => provider.GetRequiredService<HttpTunnel>();

    public ITunnel CreateDns() => provider.GetRequiredService<DnsTunnel>();
}