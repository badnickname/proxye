using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxye.Core;

namespace Proxye;

internal sealed class TunnelHostedService(IOptions<ProxyeOptions> options, ILogger<TunnelHostedService> logger, TunnelFactory factory) : BackgroundService
{
    private const string Service = "Proxye";
    private readonly TcpListener _listener = new(IPAddress.Any, options.Value.Port);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{Service}: listen on port {Port}", Service, options.Value.Port);
        
        _listener.Start();

        await ListenTcpLoop(_listener, stoppingToken);

        _listener.Stop();
    }

    private async Task ListenTcpLoop(TcpListener tcp, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var client = await tcp.AcceptTcpClientAsync(stoppingToken);
            var tunnel = factory.Create();
            Queue(tunnel, client, stoppingToken);
        }
    }

    private static void Queue(Tunnel tunnel, TcpClient client, CancellationToken stoppingToken)
    {
        Task.Run(async () =>
        {
            try
            {
                await tunnel.RunAsync(client, stoppingToken);
            }
            finally
            {
                await tunnel.DisposeAsync();
            }
        }, stoppingToken);
    }
}
