using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Proxye.Core.Models;

namespace Proxye;

public sealed class ProxyeRules(IOptionsMonitor<ProxyeRuleOptions> options) : IRules
{
    public bool Match(string host)
    {
        var regex = options.CurrentValue.Regex;

        if (regex is null)
            return false;

        return Regex.IsMatch(host, regex);
    }

    public Host Host => options.CurrentValue.Host is not null
        ? new Host(options.CurrentValue.Host, (ushort) options.CurrentValue.Port)
        : new Host("127.0.0.1", 11);

    public void UpdateRegex(string regex)
    {
        options.CurrentValue.Regex = regex;
    }

    public void UpdateHost(Host host)
    {
        options.CurrentValue.Host = host.Address;
        options.CurrentValue.Port = host.Port;
    }
}
