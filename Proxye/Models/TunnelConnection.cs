using System.Net.Sockets;

namespace Proxye.Models;

public struct TunnelConnection
{
    public string Host { get; set; }

    public uint Port { get; set; }

    public Socket Socket { get; set; }
}