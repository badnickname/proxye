using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Proxye.Rules.Models;

public sealed class Rule
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
    public Protocol Protocol { get; set; }

    /// <summary>
    ///     Host of proxy which receives matched request
    /// </summary>
    public required string Host { get; set; }

    /// <summary>
    ///     Port of proxy which receives matched request
    /// </summary>
    public int Port { get; set; }
}