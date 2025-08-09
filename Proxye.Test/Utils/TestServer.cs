using System.Net;
using System.Net.Sockets;

namespace Proxye.Test.Utils;

public sealed class TestServer(int port) : IDisposable
{
    private Task<Socket>? _socketTask;
    private readonly TcpListener _listener = new(IPAddress.Any, port);

    public void Start()
    {
        _listener.Start();
        _socketTask = _listener.AcceptSocketAsync();
    }

    public async Task<Socket> GetClientSocket()
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(IPAddress.Loopback, port);
        return socket;
    }

    public Task<Socket> GetServerSocket() => _socketTask!;
    
    public void Dispose()
    {
        _socketTask?.Dispose();
        _listener.Dispose();
    }
}