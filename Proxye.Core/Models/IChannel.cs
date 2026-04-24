namespace Proxye.Core.Models;

public interface IChannel : IDisposable
{
    void GetHost(out Host host);

    bool IsConnected { get; }
    
    Task SendAsync(Memory<byte> bytes, CancellationToken token);

    Task<Memory<byte>> ReceiveAsync(CancellationToken token);

    Task EstablishAsync(CancellationToken token);
}