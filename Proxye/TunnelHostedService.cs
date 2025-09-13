using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxye.Tunnel;

namespace Proxye;

internal sealed class TunnelHostedService(IOptions<ProxyeOptions> options, ILogger<TunnelHostedService> logger, IProxyeFactory factory) : BackgroundService
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
            var socket = await tcp.AcceptSocketAsync(stoppingToken);
            var tunnel = factory.Create(socket);
            Queue(tunnel, stoppingToken);
        }
    }

    private void Queue(IProxyeTunnel tunnel, CancellationToken stoppingToken)
    {
        Task.Run(async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await tunnel.StartAsync(stoppingToken);
                logger.LogDebug("{Service}: connect {Host}:{Port}", Service, tunnel.Host, tunnel.Port);
                await tunnel.LoopAsync(stoppingToken);
                logger.LogDebug("{Service}: disconnect {Host}:{Port} - {Time}ms", Service, tunnel.Host, tunnel.Port, stopwatch.ElapsedMilliseconds);
            }
            finally
            {
                stopwatch.Stop();
                tunnel.Dispose();
            }
        }, stoppingToken);
    }
}