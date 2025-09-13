using Proxye;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddProxye(o =>
    {
        var proxye = builder.Configuration.GetSection("Proxye").Get<ProxyeOptions>();
        o.Dns = proxye?.Dns ?? o.Dns;
        o.Rules = proxye?.Rules ?? o.Rules;
        o.Port = proxye?.Port ?? o.Port;
        o.EnableDns = proxye?.EnableDns ?? o.EnableDns;
        o.DnsPort = proxye?.DnsPort ?? o.DnsPort;
    });

var host = builder.Build();
host.Run();