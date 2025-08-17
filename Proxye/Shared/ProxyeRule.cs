using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Proxye.Shared;

public sealed class ProxyeRule
{
    /// <summary>
    ///     Regex pattern for matching rule for host
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    ///     Domains which accessing by rule
    /// </summary>
    public string[]? Domains { get; set; }

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