using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Proxye.Helpers;
using Proxye.Interfaces;
using Proxye.Shared;
using Proxye.Rules;

namespace Proxye.Tcp;

internal sealed class HttpTunnel(IProxyeRules rules) : ITcpTunnel
{
    private static readonly byte[] HostArray = "Host: ".ToArray().Select(x => (byte) x).ToArray();
    private static readonly int HostHash = HostArray.Select(x => (int) x).Sum();
    private static readonly byte[] ConnectArray = "CONNECT ".ToArray().Select(x => (byte) x).ToArray();
    private static readonly int ConnectHash = ConnectArray.Select(x => (int) x).Sum();
    private static readonly byte[] Socks5ConnectArray = [5, 1, 0];
    private static readonly byte[] Established = $"HTTP/1.1 200 Connection Established\r\nProxy-Agent: Proxye/{Assembly.GetAssembly(typeof(ProxyeTcpTunnel))?.GetName().Version!.ToString()}\r\n\r\n".ToArray().Select(x => (byte) x).ToArray();
    
    public async Task<TunnelConnection> StartAsync(Memory<byte> received, TunnelTcpContext context)
    {
        var localBuffer = context.LocalBuffer;
        var remoteBuffer = context.RemoteBuffer;
        var token = context.CancellationToken;
        var count = received.Length;
        var socket = context.Socket;
        var response = new TunnelConnection();
        var startOf = StringHelpers.GetStartOf(count, localBuffer, HostHash, HostArray);
        response.Host = StringHelpers.Read(localBuffer.AsSpan()[(startOf + HostArray.Length)..], out var length, ':');


        var orig = response.Host;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://8.8.8.8/resolve?name={response.Host}");
            request.Headers.Host = "dns.google";
            request.Headers.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/dns-message"));
            using var httpclient = new HttpClient();
            var httpResponse = await httpclient.SendAsync(request, token);
            var jsonText = await httpResponse.Content.ReadAsStringAsync(token);
            var json = JsonSerializer.Deserialize<JsonNode>(jsonText);
            response.Host = json?["Answer"]?.AsArray()?[0]?["data"]?.GetValue<string>() ?? response.Host;
        }
        catch (Exception ex)
        {
            
        }
        
        response.Port = localBuffer[startOf + HostArray.Length + length] == ':'
            ? uint.Parse(StringHelpers.Read(localBuffer.AsSpan()[(startOf + HostArray.Length + length + 1)..], out _))
            : 80;

        var isHttps = StringHelpers.GetStartOf(count, localBuffer, ConnectHash, ConnectArray) > -1;
        var rule = rules.Match(orig);
        switch (rule?.Protocol)
        {
            case ProxyeProtocol.HTTP:
                // Just send all data to another proxy
                response.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await response.Socket.ConnectAsync(rule.Host, rule.Port, token);
                break;
            case ProxyeProtocol.SOCKS5:
                if (isHttps)
                {
                    await socket.SendAsync(Established, token);
                    count = await socket.ReceiveAsync(localBuffer, token);
                }
                response.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await response.Socket.ConnectAsync(rule.Host, rule.Port, token);
                await response.Socket.SendAsync(Socks5ConnectArray, token);
                await response.Socket.ReceiveAsync(remoteBuffer, token); // todo: handle answer

                remoteBuffer[0] = 5;
                remoteBuffer[1] = 1;
                remoteBuffer[2] = 0;
                remoteBuffer[3] = 3;
                remoteBuffer[4] = (byte) response.Host.Length;
                for (var i = 0; i < response.Host.Length; i++)
                {
                    remoteBuffer[5 + i] = (byte) response.Host[i];
                }

                remoteBuffer[5 + response.Host.Length] = (byte) (response.Port >> 8);
                remoteBuffer[6 + response.Host.Length] = (byte) (response.Port & 0xff);
                await response.Socket.SendAsync(remoteBuffer.AsMemory()[..(7 + response.Host.Length)], token);
                await response.Socket.ReceiveAsync(remoteBuffer, token); // todo: handle answer
                break;
            default:
                // Send 200 OK to client if it's https request
                if (isHttps)
                {
                    await socket.SendAsync(Established, token);
                    count = await socket.ReceiveAsync(localBuffer, token);
                }
                response.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await response.Socket.ConnectAsync(response.Host, (int) response.Port, token);
                break;
        }

        await response.Socket.SendAsync(localBuffer.AsMemory()[..count], token);
        return response;
    }
}