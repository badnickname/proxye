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

    public async Task RunAsync(TcpClient client, CancellationToken token)
    {
        var cancellationToken = CancellationTokenSource
            .CreateLinkedTokenSource(token, _cancellationTokenSource.Token).Token;

        _timer = new Timer(_ => _cancellationTokenSource.Cancel());
        ResetTimer();

        using var inChannel = await inFactory.EstablishAsync(client, _inBuffer, cancellationToken);

        inChannel.GetHost(out var host);
        using var outChannel = rules.Match(host.Address)
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

    private async Task PassAsync(IChannel from, IChannel to, Action reset, CancellationToken token)
    {
        while (!token.IsCancellationRequested && from.IsConnected && to.IsConnected)
        {
            var bytes = await from.ReceiveAsync(token);

            if (bytes.Length <= 0 || !from.IsConnected || !to.IsConnected) continue;

            await to.SendAsync(bytes, token);
            reset();
        }

        if (!_cancellationTokenSource.IsCancellationRequested)
            await _cancellationTokenSource.CancelAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_timer is not null)
            await _timer.DisposeAsync();
        ArrayPool<byte>.Shared.Return(_inBuffer);
        ArrayPool<byte>.Shared.Return(_outBuffer);
    }
}