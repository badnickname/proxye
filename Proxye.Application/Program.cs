using Proxye;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddProxye()
    .AddProxyeHostedListener()
    .Configure<ProxyeOptions>(builder.Configuration.GetSection("Proxye"));

var host = builder.Build();
host.Run();