using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Proxye.Helpers;
using Proxye.Interfaces;
using Proxye.Shared;
using Proxye.Rules;

namespace Proxye.Udp;

internal sealed class DnsTunnel(IOptions<ProxyeOptions> options, IProxyeRules rules) : IUdpTunnel
{
    private static readonly ObjectPool<StringBuilder> Pool = ObjectPool.Create<StringBuilder>();

    public async Task<TunnelConnection> StartAsync(Memory<byte> received, TunnelUdpContext context)
    {
        var dns = options.Value.DnsHost;
        
        var remote = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        await remote.ConnectAsync(dns, 53);
        await remote.SendAsync(received, context.CancellationToken);
        
        return new TunnelConnection
        {
            Host = dns,
            Port = 53,
            Socket = remote
        };
    }

    public ValueTask<int> TunnelLocal(Memory<byte> received, TunnelUdpContext context)
    {
        var sb = Pool.Get();
        try
        {
            var udp = received.Span[7..]; // skip udp header

            // skip headers part
            var span = udp[11..];

            // skip queries part
            var length = 0;
            for (var i = 0; i < span.Length && span[i] != 192; i++)
            {
                length++;
            }

            span = span[length..];

            // receive first name from response
            var position = span[1];
            var nameSpan = received.Span[position..];
            var host = ReadHost(nameSpan);

            // skip name, type and class
            span = span[6..];

            // ttl in seconds
            var ttl = (span[0] << 24) + (span[1] << 16) + (span[2] << 8) + span[3];
            span = span[4..];

            // get ip length
            var ipLength = (span[0] << 8) | span[1];
            span = span[2..];

            // read and bind ip
            switch (ipLength)
            {
                case 4:
                    var ipv4 = ReadIpv4(span[..ipLength]);
                    rules.BindIp(host, ipv4, TimeSpan.FromSeconds(ttl));
                    break;
                case 16:
                    var ipv6 = ReadIpv6(span[..ipLength]);
                    rules.BindIp(host, ipv6, TimeSpan.FromSeconds(ttl));
                    break;
            }
        }
        finally
        {
            sb.Clear();
            Pool.Return(sb);
        }

        return context.Socket.SendAsync(received, context.ReceiveResult.RemoteEndPoint, context.CancellationToken);
    }

    private static string ReadIpv4(Span<byte> buffer) => $"{buffer[0]}.{buffer[1]}.{buffer[2]}.{buffer[3]}";
    
    private static string ReadIpv6(Span<byte> buffer) =>
        $"{buffer[0]:X2}{buffer[1]:X2}:{buffer[2]:X2}{buffer[3]:X2}:{buffer[4]:X2}{buffer[5]:X2}:{buffer[6]:X2}{buffer[7]:X2}:{buffer[8]:X2}{buffer[9]:X2}:{buffer[10]:X2}{buffer[11]:X2}:{buffer[12]:X2}{buffer[13]:X2}:{buffer[14]:X2}{buffer[15]:X2}";

    private static string ReadHost(Span<byte> buffer)
    {
        var sb = Pool.Get();
        try
        {
            for (var i = 0; i < buffer.Length && buffer[i] != 0; i++)
            {
                var count = buffer[i];
                sb.Append(StringHelpers.Read(buffer.Slice(i + 1, count), out _));
                sb.Append('.');
                i += count;
            }
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }
        finally
        {
            sb.Clear();
            Pool.Return(sb);
        }
    }
}