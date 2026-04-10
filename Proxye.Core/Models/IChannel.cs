namespace Proxye.Core.Models;

public interface IChannel
{
    void GetHost(out Host host);
    
    Task SendAsync(Memory<byte> bytes, CancellationToken token);

    Task<Memory<byte>> ReceiveAsync(CancellationToken token);

    Task EstablishAsync(CancellationToken token);
}