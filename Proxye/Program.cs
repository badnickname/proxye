using System.Net;
using System.Net.Sockets;
using Proxye;
using Proxye.Core;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTransient<TcpListener>(_ => new TcpListener(IPAddress.Any, 1234));
builder.Services.AddHostedService<Worker>();
builder.Services.Configure<ProxyeOptions>(builder.Configuration.GetSection("Proxye"));

var host = builder.Build();
host.Run();