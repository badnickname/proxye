using System.Net.Sockets;

namespace Proxye;

public interface IProxyeTunnel : IDisposable
{
    /// <summary>
    ///     Create tunnel
    /// </summary>
    Task StartAsync(CancellationToken token = default);

    /// <summary>
    ///     Exchanging data through tunnel
    /// </summary>
    Task LoopAsync(CancellationToken token = default);

    string Host { get; }

    uint Port { get; }

    Socket? RemoteSocket { get; }
}