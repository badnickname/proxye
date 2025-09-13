using System.Buffers;
using System.Net.Sockets;
using Proxye.Interfaces;
using Proxye.Shared;

namespace Proxye.Udp;

internal sealed class UdpProxyeTunnel(UdpReceiveResult result, UdpClient client, ITunnelFactory factory) : IProxyeTunnel
{
    private byte[]? _localBuffer;
    private IUdpTunnel? _tunnel;
    private TunnelUdpContext _context;
    private static readonly SemaphoreSlim Semaphore = new(1);

    public async Task StartAsync(CancellationToken token = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://1.1.1.1/dns-query");
        request.Headers.Host = "cloudflare-dns.com";
        request.Content = new ByteArrayContent(result.Buffer);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/dns-message");
        using var httpclient = new HttpClient();
        var response = await httpclient.SendAsync(request, token);
        var responseBytes = await response.Content.ReadAsByteArrayAsync(token);
        await client.SendAsync(responseBytes.AsMemory(), result.RemoteEndPoint, token);

        // _localBuffer = ArrayPool<byte>.Shared.Rent(65535);
        // _context = new TunnelUdpContext
        // {
        //     Socket = client,
        //     ReceiveResult = result,
        //     CancellationToken = token,
        // };
        // _tunnel = factory.CreateDns();
        // var response = await _tunnel.StartAsync(result.Buffer.AsMemory(), _context);
        // _context.RemoteSocket = response.Socket;
        // RemoteSocket = _context.RemoteSocket;
        // Host = response.Host;
        // Port = response.Port;
    }

    public async Task LoopAsync(CancellationToken token = default)
    {
                
        //
        //
        //
        // using var client = new HttpClient();
        // var response = await client.SendAsync(request);
        //
        // var count = await RemoteSocket!.ReceiveAsync(_localBuffer, token);
        // if (token.IsCancellationRequested) return;
        // await Semaphore.WaitAsync(token);
        // try
        // {
        //     await _tunnel!.TunnelLocal(_localBuffer.AsMemory()[..count], _context);
        // }
        // finally
        // {
        //     Semaphore.Release();
        // }
    }

    public string Host { get; private set; }

    public uint Port { get; private set; }

    public Socket? RemoteSocket { get; private set; }

    public void Dispose()
    {
        if (_localBuffer is not null) ArrayPool<byte>.Shared.Return(_localBuffer);
        RemoteSocket?.Dispose();
    }
}