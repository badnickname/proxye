# Proxye 🌐
Proxye is a simple, lightweight HTTP/SOCKS5 proxy for .NET designed to help you redirect requests seamlessly through other proxies or services. Perfect for home networking setups!

## Overview ℹ️
Proxye allows you to create a flexible proxy server that supports custom redirection rules, making it easy to route your traffic through various proxies or services. It features:
- .NET native implementation which easy integrated in ASP.NET Core projects
- Custom redirect rules based on domains or URL patterns 📃
- Built-in DNS proxy with DNS over HTTPS (DoH) tunneling for secure name resolution 🔐

## Get Started 🚀
Follow these steps to set up Proxye in your project:

### 1. Install via NuGet
Run this in your terminal or Package Manager Console:
```shell
dotnet add package Proxye --version 0.0.3
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
    o.Rules = new List<Rule>
    {
        new Rule
        {
            Domains = new[] { "2ip.io" }, // domain match
            Pattern = ".*(2ip).*",       // pattern match
            
            // Redirect to another proxy
            Host = "127.0.0.1",
            Port = 1080,
            Protocol = Protocol.SOCKS5
        }
    };
});
```

### 3. Run your application
Start your project, and the proxy server will be ready to route requests based on your rules! 🚥

## Note: Early Alpha Release ⚠️
Proxye is currently in early stages of development. 💡 This means it is still under active development, and some features may be unstable or incomplete. Your feedback is highly appreciated to help improve and stabilize the project