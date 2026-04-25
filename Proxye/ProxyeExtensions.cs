using Microsoft.Extensions.DependencyInjection;
using Proxye.Core;
using Proxye.Core.Implementations;
using Proxye.Core.Models;
using Proxye.Dns;

namespace Proxye;

public static class ProxyeExtensions
{
    public static IServiceCollection AddProxye(this IServiceCollection services, Action<ProxyeOptions>? configure = null)
    {
        var options = new ProxyeOptions();
        configure?.Invoke(options);

        services
            .AddDns()
            .AddSingleton<TunnelFactory>()
            .AddSingleton<InChannelFactory>()
            .AddSingleton<OutChannelFactory>()
            .AddSingleton<ProxyeRules>()
            .AddSingleton<IRules>(sp => sp.GetRequiredService<ProxyeRules>())
            .AddOptions<ProxyeOptions>().Configure(o => configure?.Invoke(o)).Services
            .Configure<DnsOptions>(o =>
            {
                o.Url = options.Dns.Url;
                o.BaseTtl = options.Dns.BaseTtl;
            })
            .AddHostedService<TunnelHostedService>();

        if (options.EnableDns) services.AddHostedService<DnsHostedService>();
        services.AddHostedService<TunnelHostedService>();
        return services;
    }
}