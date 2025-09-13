using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Proxye.Dns;

/// <summary>
///     Resolves DNS queries using an HTTP-based DNS resolver with caching
/// </summary>
public interface IDnsResolver
{
    /// <summary>
    ///     Resolve the given host to an IP address, using caching to minimize repeated lookups
    /// </summary>
    ValueTask<string> Resolve(string host, CancellationToken token);
}

internal sealed class DnsResolver(IHttpClientFactory factory, IMemoryCache cache, IOptions<DnsOptions> options) : IDnsResolver
{
    public async ValueTask<string> Resolve(string host, CancellationToken token)
    {
        if (cache.TryGetValue(host, out var result)) return (string) result!;

        var client = factory.CreateClient("dns");
        var request = new HttpRequestMessage(HttpMethod.Get, $"{options.Value.Url}?name={host}");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/dns-message"));
        var httpResponse = await client.SendAsync(request, token);
        var jsonText = await httpResponse.Content.ReadAsStringAsync(token);
        var json = JsonSerializer.Deserialize<JsonNode>(jsonText);

        // todo return original host if it's already an IP address

        // 0: NoError, 2: ServFail, 3: NXDomain
        var status = json?["Status"]?.GetValue<int>() ?? 2;
        // If the DNS query failed, cache the original host for a short period to avoid repeated lookups
        if (status != 0)
        {
            cache.Set(host, result, TimeSpan.FromSeconds(options.Value.BaseTtl));
            return host;
        }

        var array = json?["Answer"]?.AsArray();
        if (array is null || array.Count == 0) return host;

        var data = array[0]?.AsObject();

        if (data is null) return host;

        var ttl = data["TTL"]?.GetValue<int>() ?? options.Value.BaseTtl;
        result = data["data"]?.GetValue<string>() ?? host;

        cache.Set(host, result, TimeSpan.FromSeconds(ttl));
        return (string) result;
    }
}