namespace Proxye.Core;

public sealed class ProxyeRule
{
    public string Pattern { get; set; }

    public string Protocol { get; set; }

    public string Host { get; set; }

    public int Port { get; set; }
}