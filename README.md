# Proxye 🌐
Proxye is a lightweight HTTP proxy for .NET designed to redirect requests matching regex patterns to a SOCKS5 proxy (with potential Shadowsocks support in future versions)  

## Key Features ℹ️
- Working as HTTP-proxy
- Traffic filtering via regex matching
- Easy integrated in ASP.NET Core projects
- Built-in DNS proxy with DNS over HTTPS (DoH) tunneling for secure name resolution

## Get Started 🚀
Follow these steps to set up Proxye in your project:

### 1. Install via NuGet
Run this in your terminal or Package Manager Console:
```shell
dotnet add package Proxye --version 0.1.0
```

### 2. Configure and Run
Here's a basic example to get you started:
```csharp
builder.Services.AddProxye(o =>
{
    // DNS configuration
    o.Dns = new DnsOptions
    {
        BaseTtl = 3600,
        Url = "https://dns.google/resolve"
    };
    o.EnableDns = true; // enable DNS tunneling
    o.DnsPort = 52; // DNS listener port
    
    // Proxy server configuration
    o.Port = 9567; // proxy listening port
    o.Rules = new ProxyeRuleOptions
    {
        Regex = ".*(google|youtube).*", // domains which be redirected to socks5
        Host = "127.0.0.1", // socks5 host
        Port = 1080 // socks5 port
    };
});
```

### 3. Run your application
Start your project, and the proxy server will be ready to route requests based on your rules! 🚥

## Note: Early Alpha Release ⚠️
Proxye is currently in early stages of development. 💡 This means it is still under active development, and some features may be unstable or incomplete. Your feedback is highly appreciated to help improve and stabilize the project