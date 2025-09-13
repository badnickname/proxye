namespace Proxye.Dns;

/// <summary>
///     Options for DNS resolution
/// </summary>
public sealed class DnsOptions
{
    /// <summary>
    ///     External DNS over HTTPS (DoH) server URL
    /// </summary>
    public string Url { get; set; } = "https://dns.google/resolve";

    /// <summary>
    ///     Base TTL for DNS records in seconds
    /// </summary>
    public int BaseTtl { get; set; } = 3600;
}