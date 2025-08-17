using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Proxye.Tunnels;

namespace Proxye;

public interface IProxyeFactory
{
    /// <summary>
    ///     Create tunnel
    /// </summary>
    IProxyeTunnel CreateTcp(Socket socket);

    /// <summary>
    ///     Create udp-tunnel
    /// </summary>
    IProxyeTunnel CreateUdp(UdpClient client, UdpReceiveResult result);
}

internal sealed class ProxyeFactory(IServiceProvider provider) : IProxyeFactory
{
    public IProxyeTunnel CreateTcp(Socket socket)
    {
        return new ProxyeTcpTunnel(socket, provider.GetRequiredService<ITunnelFactory>());
    }

    public IProxyeTunnel CreateUdp(UdpClient client, UdpReceiveResult result)
    {
        return new UdpProxyeTunnel(result, client, provider.GetRequiredService<ITunnelFactory>());
    }
}