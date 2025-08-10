using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace Proxye;

public interface IProxyeFactory
{
    /// <summary>
    ///     Create tunnel
    /// </summary>
    IProxyeTunnel Create(Socket socket);
}

public sealed class ProxyeFactory(IOptions<ProxyeOptions>? options) : IProxyeFactory
{
    public IProxyeTunnel Create(Socket socket)
    {
        return new ProxyeTunnel(socket, options?.Value ?? new ProxyeOptions { Rules = [] });
    }
}