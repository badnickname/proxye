using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace Proxye.Test.Utils;

public static class TestContainer
{
    public static IServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddProxye();
        return services.BuildServiceProvider();
    }

    public static IProxyeTunnel CreateTunnel(Socket socket)
    {
        var provider = CreateProvider();
        var factory = provider.GetRequiredService<IProxyeFactory>();
        return factory.Create(socket);
    }
}