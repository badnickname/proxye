using Microsoft.Extensions.DependencyInjection;

namespace Proxye;

public static class ProxyeExtensions
{
    public static IServiceCollection AddProxye(this IServiceCollection services, Action<ProxyeOptions>? configure = null)
    {
        services.AddSingleton<IProxyeFactory, ProxyeFactory>();
        services.AddOptions<ProxyeOptions>();
        if (configure is not null) services.Configure(configure);
        return services;
    }
}