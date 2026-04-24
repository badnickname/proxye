namespace Proxye.Core.Models;

public interface IRules
{
    bool Match(string host);
    
    Host Host { get; }
}
