using System.Net.Sockets;

namespace Proxye.Models;

internal struct TunnelUdpContext
{
    public UdpClient Socket { get; init; }

    public Socket? RemoteSocket { get; set; }

    public byte[] LocalBuffer { get; init; }

    public UdpReceiveResult ReceiveResult { get; init; }

    public CancellationToken CancellationToken { get; init; }
}