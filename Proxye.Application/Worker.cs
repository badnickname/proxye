using System.Net.Sockets;

namespace Proxye.Application;

public class Worker(TcpListener listener, IProxyeFactory factory, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tunnels = new List<IProxyeTunnel>();
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

    private async Task LoopAsync(List<IProxyeTunnel> tunnels, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var socket = await listener.AcceptSocketAsync(stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;
            var tunnel = factory.Create(socket);
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