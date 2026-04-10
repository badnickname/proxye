using Proxye.Core.Models;

namespace Proxye.Core.Implementations.Channel;

public class DirectOutChannel(Host host, Memory<byte> buffer) : IChannel
{
    public async Task EstablishAsync(CancellationToken token)
    {
        
    }

    public void GetHost(out Host host)
    {
        throw new NotImplementedException();
    }

    public async Task SendAsync(Memory<byte> bytes, CancellationToken token)
    {
        
    }
    
    public async Task<Memory<byte>> ReceiveAsync(CancellationToken token)
    {
        
    }
}