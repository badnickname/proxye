using System.Net.Sockets;
using Microsoft.Extensions.Options;
using Proxye.Core;

namespace Proxye;

public class Worker(TcpListener listener, IOptions<ProxyeOptions> options, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tunnels = new List<Tunnel>();
        listener.Start();
        try
        {
            await LoopAsync(tunnels, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            
        }
        listener.Stop();
        lock (tunnels)
        {
            logger.LogWarning("Unfinished tunnels count: {Count}", tunnels.Count);
            foreach (var tunnel in tunnels)
            {
                logger.LogWarning("Tunnel disposed {Host}:{Port}", tunnel.Host, tunnel.Port);
                tunnel.Dispose();
            }
        }
    }

    private async Task LoopAsync(List<Tunnel> tunnels, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var socket = await listener.AcceptSocketAsync(stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;
            var tunnel = new Tunnel(socket, options.Value);
            Task.Run(async () =>
            {
                try
                {
                    await tunnel.StartAsync(stoppingToken);
                    logger.LogInformation("Tunnel connected to {Host}:{Port}", tunnel.Host, tunnel.Port);
                    lock (tunnels)
                    {
                        tunnels.Add(tunnel);
                    }

                    await tunnel.LoopAsync(stoppingToken);
                    lock (tunnels)
                    {
                        logger.LogWarning("Tunnel disposed {Host}:{Port}", tunnel.Host, tunnel.Port);
                        tunnels.Remove(tunnel);
                    }
                }
                catch (Exception) { }
                finally
                {
                    tunnel.Dispose();
                }
            });
        }
    }
}