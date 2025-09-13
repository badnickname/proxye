using Proxye.Dns;
using Proxye.Rules.Models;

namespace Proxye;

public sealed class ProxyeOptions
{
    public Rule[] Rules { get; set; } = [];

    public int Port { get; set; } = 9567;

    public bool EnableDns { get; set; } = true;

    public DnsOptions Dns { get; set; } = new();

    public int DnsPort { get; set; } = 9568;
}
