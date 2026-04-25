using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Proxye.Core.Models;

namespace Proxye;

public sealed class ProxyeRules(IOptionsMonitor<ProxyeOptions> options) : IRules
{
    public bool Match(string host)
    {
        var regex = options.CurrentValue.Rules.Regex;

        return regex is not null && Regex.IsMatch(host, regex);
    }

    public Host Host => options.CurrentValue.Rules.Host is not null
        ? new Host(options.CurrentValue.Rules.Host, (ushort) options.CurrentValue.Port)
        : new Host("127.0.0.1", 11);

    public void UpdateRegex(string regex)
    {
        options.CurrentValue.Rules.Regex = regex;
    }

    public void UpdateHost(Host host)
    {
        options.CurrentValue.Rules.Host = host.Address;
        options.CurrentValue.Rules.Port = host.Port;
    }
}
