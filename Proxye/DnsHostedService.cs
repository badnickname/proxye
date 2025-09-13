using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proxye.Dns;

namespace Proxye;

public sealed class DnsHostedService(IOptions<ProxyeOptions> options, ILogger<DnsHostedService> logger, IDnsTunnel dns) : BackgroundService
{
    private const string Service = "Proxye DNS";
    private readonly UdpClient _client = new(options.Value.DnsPort);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{Service}: listen on port {Port}", Service, options.Value.DnsPort);

        await ListenUdpLoop(_client, stoppingToken);
    }

    private async Task ListenUdpLoop(UdpClient upd, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var income = await upd.ReceiveAsync(stoppingToken);
                var (host, bytes) = await dns.Tunnel(income.Buffer, stoppingToken);
                await upd.SendAsync(bytes, income.RemoteEndPoint, stoppingToken);
                logger.LogDebug("{Service}: discovered {Host} - {Time}ms", Service, host, stopwatch.ElapsedMilliseconds);
            }
            finally
            {
                stopwatch.Stop();
            }
        }
    }
}