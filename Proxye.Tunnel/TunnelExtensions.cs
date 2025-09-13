using Microsoft.Extensions.DependencyInjection;
using Proxye.Tunnel.Protocols;

namespace Proxye.Tunnel;

public static class TunnelExtensions
{
    public static IServiceCollection AddTunnel(this IServiceCollection services) => services
        .AddSingleton<IProtocolFactory, ProtocolFactory>()
        .AddTransient<Http>()
        .AddTransient<Socks5>()
        .AddSingleton<IProxyeFactory, ProxyeFactory>()
        .AddOptions<TunnelOptions>().Services;
}