using System.Text.RegularExpressions;
using Proxye.Core.Collections;

namespace Proxye.Core;

public sealed class ProxyeOptions
{
    private readonly BoundedDictionary<string, ProxyeRule?> _map = new(1000);

    public ProxyeRule[] Rules { get; set; } = [];

    internal ProxyeRule? Match(string host)
    {
        if (_map.TryGetValue(host, out var result))
        {
            return result;
        }

        foreach (var rule in Rules)
        {
            var regex = new Regex(rule.Pattern);
            if (!regex.IsMatch(host)) continue;
            _map[host] = rule;
            return rule;
        }
        _map[host] = null;

        return null;
    }
}
