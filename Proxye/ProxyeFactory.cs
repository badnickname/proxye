using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Proxye.Tunnels;

namespace Proxye;

public interface IProxyeFactory
{
    /// <summary>
    ///     Create tunnel
    /// </summary>
    IProxyeTunnel Create(Socket socket);
}

internal sealed class ProxyeFactory(IServiceProvider provider) : IProxyeFactory
{
    public IProxyeTunnel Create(Socket socket)
    {
        return new ProxyeTunnel(socket, provider.GetRequiredService<ITunnelFactory>());
    }
}