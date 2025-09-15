# Proxye
Simple HTTP/SOCKS5 proxy for .NET which able to redirect requests to another proxies  

## Features
1. Setting redirecting rules by custom configuration
2. Built-in DNS proxy which tunneling requests over HTTPS (DoH)

## Installation
```bash
dotnet add package Proxye --version 0.0.3
```

## Configuration
```csharp
builder.Services.AddProxye(o =>
{
    // DNS configuration
    o.Dns = new DnsOptions
    {
        BaseTtl = 3600, // base time to live (if it wasn't received from DNS server)
        Url = "https://dns.google/resolve" // DNS resolver
    };
    o.EnableDns = true; // set false to disable DNS listener
    o.DnsPort = 52; // port of DNS listener
    
    // Proxy configuration
    o.Port = 9567; // port of proxy
    o.Rules = // rules for redirecting
    [
        new Rule
        {
            // requests matched this rule will be redirected
            Domains = ["2ip.io"], // match Host name by domain
            Pattern = ".*(2ip).*", // or match Host name by pattern
            
            // configuration of proxy for redirecting
            Host = "127.0.0.1", // host of another proxy
            Port = 1080, // port of another proxy
            Protocol = Protocol.SOCKS5 // protocol of another proxy
        }
    ];
});
```
