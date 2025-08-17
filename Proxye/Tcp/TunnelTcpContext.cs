using System.Net.Sockets;

namespace Proxye.Tcp;

public struct TunnelTcpContext
{
    public Socket Socket { get; init; }

    public Socket? RemoteSocket { get; set; }

    public byte[] LocalBuffer { get; init; }

    public byte[] RemoteBuffer { get; init; }

    public CancellationToken CancellationToken { get; init; }
}