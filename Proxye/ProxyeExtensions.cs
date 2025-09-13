using Microsoft.Extensions.DependencyInjection;
using Proxye.Dns;
using Proxye.Rules;
using Proxye.Rules.Models;
using Proxye.Tunnel;

namespace Proxye;

public static class ProxyeExtensions
{
    public static IServiceCollection AddProxye(this IServiceCollection services, Action<ProxyeOptions>? configure = null)
    {
        var options = new ProxyeOptions();
        configure?.Invoke(options);

        services
            .AddDns()
            .AddRules()
            .AddTunnel()
            .AddOptions<ProxyeOptions>().Configure(o => configure?.Invoke(o)).Services
            .Configure<List<Rule>>(o => o.AddRange(options.Rules))
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