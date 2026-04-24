using System.Text;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Proxye.Dns;

/// <summary>
///     Interface for tunneling DNS queries and responses
/// </summary>
public interface IDnsTunnel
{
    /// <summary>
    ///     Tunnel DNS queries and responses
    /// </summary>
    Task<(string Host, Memory<byte> Bytes)> Tunnel(Memory<byte> data, CancellationToken cancellationToken);
}

internal sealed class DnsTunnel(IHttpClientFactory factory, IOptions<DnsOptions> options) : IDnsTunnel
{
    private static readonly ObjectPool<StringBuilder> Pool = ObjectPool.Create<StringBuilder>();
    
    public async Task<(string Host, Memory<byte> Bytes)> Tunnel(Memory<byte> data, CancellationToken cancellationToken)
    {
        var client = factory.CreateClient("dns");
        var request = new HttpRequestMessage(HttpMethod.Post, options.Value.Url);
        request.Content = new ByteArrayContent(data.ToArray()); // todo fix allocation
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/dns-message");
        var response = await client.SendAsync(request, cancellationToken);
        var responseBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        var host = Analyze(responseBytes);

        return (host, responseBytes);
    }

    private string Analyze(byte[] received)
    {
        var sb = Pool.Get();
        try
        {
            var udp = received[7..]; // skip udp header

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
            var nameSpan = received[position..];
            var host = ReadHost(nameSpan);

            return host;
        }
        finally
        {
            sb.Clear();
            Pool.Return(sb);
        }
    }

    private static string ReadHost(Span<byte> buffer)
    {
        var sb = Pool.Get();
        try
        {
            for (var i = 0; i < buffer.Length && buffer[i] != 0; i++)
            {
                var count = buffer[i];
                sb.Append(Read(buffer.Slice(i + 1, count), out _));
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

    private static string Read(Span<byte> buffer, out int lenght, char endChar = '\0')
    {
        var sb = Pool.Get();
        try
        {
            lenght = 0;
            foreach (var t in buffer)
            {
                if (t != '\r' && t != '\n' && t != '\0' && t != endChar)
                {
                    lenght++;
                    sb.Append((char) t);
                }
                else
                {
                    break;
                }
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