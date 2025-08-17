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
            // skip queries part
            var span = received.Span;
            var count = (span[4] << 8) + span[5];
            var length = 0;
            for (var i = 0; i < count; i++)
            {
                for (var j = i; j < span.Length; j += span[j] + 1)
                {
                    length += span[j] + 1;
                    if (span[j] == 0) break;
                }
                length += 4;
            }

            // receive first ip from answer
            var answer = span[(12 + length)..];

            var reference = (answer[0] << 8) + answer[1];
            var host = ReadHost(span[reference..]);

            var ttl = ((long)answer[6] << 24) + ((long)answer[7] << 16) + (answer[8] << 8) + answer[9];

            var len = answer[10] + (answer[11] << 8);

            if (len == 4)
            {
                var ip = ReadIpv4(answer.Slice(12, 4));
                rules.BindIp(host, ip, TimeSpan.FromSeconds(ttl));
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

    private static string ReadHost(Span<byte> buffer)
    {
        var sb = Pool.Get();
        try
        {
            for (var i = 0; i < buffer.Length && buffer[i] != 0; i++)
            {
                var count = buffer[i];
                sb.Append(StringHelpers.Read(buffer.Slice(i, count), out _));
                i += count;
            }
            return sb.ToString();
        }
        finally
        {
            sb.Clear();
            Pool.Return(sb);
        }
    }
}