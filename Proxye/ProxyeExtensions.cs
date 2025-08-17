using Microsoft.Extensions.DependencyInjection;
using Proxye.Rules;
using Proxye.Services;
using Proxye.Shared;
using Proxye.Tcp;
using Proxye.Udp;

namespace Proxye;

public static class ProxyeExtensions
{
    public static IServiceCollection AddProxye(this IServiceCollection services, Action<ProxyeOptions>? configure = null)
    {
        services.AddSingleton<IProxyeFactory, ProxyeFactory>();
        services.AddTransient<DnsTunnel>();
        services.AddTransient<Socks5Tunnel>();
        services.AddTransient<HttpTunnel>();
        services.AddSingleton<IProxyeRules, ProxyeRules>();
        services.AddSingleton<ITunnelFactory, TunnelFactory>();
        services.AddOptions<ProxyeOptions>();
        if (configure is not null) services.Configure(configure);
        return services;
    }

    public static IServiceCollection AddProxyeHostedListener(this IServiceCollection services)
    {
        services.AddHostedService<ProxyeHostedService>();
        return services;
    }
}