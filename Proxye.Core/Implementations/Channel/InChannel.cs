using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using Proxye.Core.Models;

namespace Proxye.Core.Implementations.Channel;

internal sealed class InChannel(TcpClient client, Memory<byte> buffer) : IChannel
{
    private static readonly byte[] HostArray = "Host: ".ToArray().Select(x => (byte) x).ToArray();
    private static readonly int HostHash = HostArray.Select(x => (int) x).Sum();
    private static readonly byte[] ConnectArray = "CONNECT ".ToArray().Select(x => (byte) x).ToArray();
    private static readonly int ConnectHash = ConnectArray.Select(x => (int) x).Sum();
    private static readonly ObjectPool<StringBuilder> Pool = ObjectPool.Create<StringBuilder>();
    private static readonly byte[] Established = "HTTP/1.1 200 Connection Established\r\nProxy-Agent: Proxye 1.0.0\r\n\r\n".ToArray().Select(x => (byte) x).ToArray();

    private Host? _host;
    private int _count;
    private readonly SemaphoreSlim _semaphoreSlim = new(0, 1);
    private readonly NetworkStream _stream = client.GetStream();
    private bool _isDisconnected;

    public async Task EstablishAsync(CancellationToken token)
    {
        var count = await _stream.ReadAsync(buffer, token);

        var startOf = GetStartOf(count, buffer.Span, HostHash, HostArray);

        var host = Read(buffer.Span[(startOf + HostArray.Length)..], out var length, ':');
        var port = buffer.Span[startOf + HostArray.Length + length] == ':'
            ? uint.Parse(Read(buffer.Span[(startOf + HostArray.Length + length + 1)..], out _))
            : 80;
        _host = new Host(host, (ushort) port);

        var isHttps = GetStartOf(count, buffer.Span, ConnectHash, ConnectArray) > -1;

        if (isHttps)
        {
            await _stream.WriteAsync(Established, token);
        }
        else
        {
            _count = count;
        }
    }

    public void GetHost(out Host host)
    {
        host = _host ?? default;
    }

    public async Task SendAsync(Memory<byte> bytes, CancellationToken token)
    {
        await WaitWhenHttpRequestSent(token);

        if (bytes.Length == 0) return;

        await _stream.WriteAsync(bytes, token);
    }

    public bool IsConnected => !_isDisconnected && _stream is { CanRead: true, CanWrite: true } && client.Connected;

    public async Task<Memory<byte>> ReceiveAsync(CancellationToken token)
    {
        if (TryReturnHttpRequest(out var request))
        {
            return request.Value;
        }

        var receive = await _stream.ReadAsync(buffer, token);
        if (receive < 1)
            _isDisconnected = true;

        return buffer[..receive];
    }
    
    public void Dispose()
    {
        _semaphoreSlim.Dispose();
        _stream.Dispose();
        client.Dispose();
    }

    private async Task WaitWhenHttpRequestSent(CancellationToken token)
    {
        if (_count > 0)
        {
            await _semaphoreSlim.WaitAsync(token);
            _semaphoreSlim.Release();
        }
    }

    private bool TryReturnHttpRequest([NotNullWhen(true)] out Memory<byte>? request)
    {
        if (_count > 0)
        {
            request = buffer[.._count];

            _semaphoreSlim.Release();

            _count = 0;

            return true;
        }

        request = null;
        return false;
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

    private static int GetStartOf(int count, Span<byte> buffer, int targetHash, Span<byte> target)
    {
        var hash = 0;
        for (var i = 0; i < target.Length; i++) hash += buffer[i];
        for (var i = 0; i < count - target.Length; i++)
        {
            if (hash == targetHash)
            {
                for (var j = 0; j < target.Length; j++)
                {
                    if (buffer[i + j] != target[j]) goto failed;
                }
                return i;
            }

            failed:
            hash -= buffer[i];
            hash += buffer[i + target.Length];
        }

        return -1;
    }
}