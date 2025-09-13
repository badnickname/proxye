using Microsoft.Extensions.DependencyInjection;

namespace Proxye.Dns;

public static class DnsExtensions
{
    /// <summary>
    ///     Add DNS resolver and DNS tunnel services
    /// </summary>
    public static IServiceCollection AddDns(this IServiceCollection services)
        => services
            .AddSingleton<IDnsResolver, DnsResolver>()
            .AddSingleton<IDnsTunnel, DnsTunnel>()
            .AddOptions<DnsOptions>().Services
            .AddMemoryCache()
            .AddHttpClient("dns").Services;
}