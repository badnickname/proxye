using System.Net.Sockets;
using Proxye.Core.Models;

namespace Proxye.Core.Implementations;

public class InChannelFactory
{
    public async Task<IChannel> EstablishAsync(Socket socket, Memory<byte> buffer, CancellationToken token)
    {
        
    }
}