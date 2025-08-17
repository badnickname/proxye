using Proxye.Models;

namespace Proxye;

public sealed class ProxyeOptions
{
    /// <summary>
    ///     Rules for redirecting request to external proxies
    /// </summary>
    public ProxyeRule[] Rules { get; set; } = [];

    public int Port { get; set; } = 9567;

    public string DnsHost { get; set; } = "1.1.1.1";
}
