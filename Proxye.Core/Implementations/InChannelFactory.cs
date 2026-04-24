using System.Net.Sockets;
using Proxye.Core.Implementations.Channel;
using Proxye.Core.Models;

namespace Proxye.Core.Implementations;

public class InChannelFactory
{
    public async Task<IChannel> EstablishAsync(TcpClient client, Memory<byte> buffer, CancellationToken token)
    {
        var channel = new InChannel(client, buffer);
        try
        {
            await channel.EstablishAsync(token);
            return channel;
        }
        catch
        {
            channel.Dispose();
            throw;
        }
    }
}
