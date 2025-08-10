using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Converters;
using Proxye.Collections;

namespace Proxye;

public sealed class ProxyeOptions
{
    private readonly BoundedDictionary<string, ProxyeRule?> _map = new(5000);
    private readonly ConcurrentDictionary<ProxyeRule, Regex> _regex = new();

    /// <summary>
    ///     Rules for redirecting request to external proxies
    /// </summary>
    public ProxyeRule[] Rules { get; set; } = [];

    internal ProxyeRule? Match(string host)
    {
        if (_map.TryGetValue(host, out var result))
        {
            return result;
        }

        foreach (var rule in Rules)
        {
            var regex = _regex.TryGetValue(rule, out var r) ? r : new Regex(rule.Pattern);
            _regex.TryAdd(rule, regex);
            if (!regex.IsMatch(host)) continue;
            _map.Add(host, rule);
            return rule;
        }
        _map.Add(host, null);

        return null;
    }
}

public sealed class ProxyeRule
{
    /// <summary>
    ///     Regex pattern for matching rule for host
    /// </summary>
    public required string Pattern { get; set; }

    /// <summary>
    ///     Protocol of proxy which receives matched request
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public ProxyeProtocol Protocol { get; set; }

    /// <summary>
    ///     Host of proxy which receives matched request
    /// </summary>
    public required string Host { get; set; }

    /// <summary>
    ///     Port of proxy which receives matched request
    /// </summary>
    public int Port { get; set; }
}

public enum ProxyeProtocol
{
    HTTP,
    SOCKS5
}