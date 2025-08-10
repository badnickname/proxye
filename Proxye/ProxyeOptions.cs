using Proxye.Rules;

namespace Proxye;

public sealed class ProxyeOptions
{
    /// <summary>
    ///     Rules for redirecting request to external proxies
    /// </summary>
    public ProxyeRule[] Rules { get; set; } = [];
}
