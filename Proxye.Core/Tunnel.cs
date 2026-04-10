using System.Buffers;
using System.Net.Sockets;
using Proxye.Core.Implementations;
using Proxye.Core.Models;

namespace Proxye.Core;

public sealed class Tunnel(IRules rules, InChannelFactory inFactory, OutChannelFactory outFactory) : IAsyncDisposable
{
    private const int PackageMaxSize = 65535;
    private readonly byte[] _inBuffer = ArrayPool<byte>.Shared.Rent(PackageMaxSize);
    private readonly byte[] _outBuffer = ArrayPool<byte>.Shared.Rent(PackageMaxSize);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Timer? _timer;

    public async Task RunAsync(Socket socket, CancellationToken token)
    {
        var cancellationToken = CancellationTokenSource
            .CreateLinkedTokenSource(token, _cancellationTokenSource.Token).Token;

        _timer = new Timer(_ => _cancellationTokenSource.Cancel());
        ResetTimer();

        var inChannel = await inFactory.EstablishAsync(socket, _inBuffer, cancellationToken);

        inChannel.GetHost(out var host);
        var outChannel = rules.Match(host.Address)
            ? outFactory.CreateProxied(host, _outBuffer)
            : outFactory.CreateDirect(host, _outBuffer);

        await outChannel.EstablishAsync(cancellationToken);

        await Task.WhenAll(
            PassAsync(inChannel, outChannel, ResetTimer, cancellationToken),
            PassAsync(outChannel, inChannel, ResetTimer, cancellationToken)
        );
    }

    private void ResetTimer()
        => _timer!.Change(TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);

    private static async Task PassAsync(IChannel from, IChannel to, Action reset, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var bytes = await from.ReceiveAsync(token);
            await to.SendAsync(bytes, token);
            reset();
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_timer is not null)
            await _timer.DisposeAsync();
        ArrayPool<byte>.Shared.Return(_inBuffer);
        ArrayPool<byte>.Shared.Return(_outBuffer);
    }
}