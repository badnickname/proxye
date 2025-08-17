using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxye.Shared;

namespace Proxye.Services;

internal sealed class ProxyeHostedService(IOptions<ProxyeOptions> options, ILogger<ProxyeHostedService> logger, IProxyeFactory factory) : BackgroundService
{
    private readonly UdpClient _dnsClient = new(53);
    private readonly TcpListener _listener = new(IPAddress.Any, options.Value.Port);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{Service}: listen DNS on port {Port}", "Proxye", 53);
        logger.LogInformation("{Service}: listen on port {Port}", "Proxye", options.Value.Port);
        _listener.Start();
       
        var dns = ListenUdpLoop(_dnsClient, stoppingToken);
        var proxy = ListenTcpLoop(_listener, stoppingToken);

        await Task.WhenAll(dns, proxy);

        _listener.Stop();
    }

    private Task ListenUdpLoop(UdpClient upd, CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var income = await upd.ReceiveAsync(stoppingToken);
                var tunnel = factory.CreateUdp(upd, income);
                Queue(tunnel, stoppingToken);
            }
        }, stoppingToken);
    }

    private Task ListenTcpLoop(TcpListener tcp, CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var socket = await tcp.AcceptSocketAsync(stoppingToken);
                var tunnel = factory.CreateTcp(socket);
                Queue(tunnel, stoppingToken);
            }
        }, stoppingToken);
    }
    
    private void Queue(IProxyeTunnel tunnel, CancellationToken stoppingToken)
    {
        Task.Run(async () =>
        {
            try
            {
                await tunnel.StartAsync(stoppingToken);
                logger.LogDebug("{Service}: connect {Host}:{Port}", "Proxye", tunnel.Host, tunnel.Port);
                await tunnel.LoopAsync(stoppingToken);
                logger.LogDebug("{Service}: disconnect {Host}:{Port}", "Proxye", tunnel.Host, tunnel.Port);
            }
            finally
            {
                tunnel.Dispose();
            }
        }, stoppingToken);
    }
}