using Microsoft.Extensions.DependencyInjection;

namespace Proxye.Tunnel.Protocols;

internal interface IProtocolFactory
{
    IProtocol CreateSocks5();

    IProtocol CreateHttp();
}

internal sealed class ProtocolFactory(IServiceProvider provider) : IProtocolFactory
{
    public IProtocol CreateSocks5() => provider.GetRequiredService<Socks5>();

    public IProtocol CreateHttp() => provider.GetRequiredService<Http>();
}