using Proxye.Dns;

namespace Proxye;

public sealed class ProxyeOptions
{
    public int Port { get; set; } = 9567;

    public bool EnableDns { get; set; } = true;

    public DnsOptions Dns { get; set; } = new();

    public int DnsPort { get; set; } = 9568;

    public ProxyeRuleOptions Rules { get; set; }
}
