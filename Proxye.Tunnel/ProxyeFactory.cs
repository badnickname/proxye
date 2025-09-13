using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Proxye.Tunnel.Protocols;

namespace Proxye.Tunnel;

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
        => new ProxyeTunnel(socket, provider.GetRequiredService<IProtocolFactory>());
}