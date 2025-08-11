using System.Net.Sockets;
using System.Text;
using Proxye.Test.Utils;

namespace Proxye.Test;

public class TunnelTest : IAsyncLifetime
{
    private readonly TestServer _testServer = CreateServer(9765);
    private Socket ClientSocket { get; set; }
    private Socket ServerSocket { get; set; }
    private byte[] Bytes { get; init; } = new byte[65535];

    public async Task InitializeAsync()
    {
        _testServer.Start();
        ClientSocket = await _testServer.GetClientSocket();
        ServerSocket = await _testServer.GetServerSocket();
    }

    private static TestServer CreateServer(int port = 80)
    {
        var server = new TestServer(port);
        server.Start();
        return server;
    }

    public Task DisposeAsync()
    {
        _testServer.Dispose();
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData(443)]
    [InlineData]
    [InlineData(123)]
    public async Task Read_Host_And_Port(int? port = null)
    {
        // Arrange
        using var remote = CreateServer(port ?? 80);
        var hostname = port.HasValue ? $"127.0.0.1:{port.Value}" : "127.0.0.1";
        var data = Encoding.ASCII.GetBytes($"GET / HTTP/1.1\r\nHost: {hostname}\r\nUser-Agent: proxye-test\r\n\r\n");
        using var tunnel = new ProxyeTunnel(ServerSocket, new ProxyeOptions());

        // Act
        await ClientSocket.SendAsync(data);                   // Send request to proxy
        await tunnel.StartAsync(CancellationToken.None);   // Receive request and get Host and port

        // Assert
        Assert.Equal("127.0.0.1", tunnel.Host);
        Assert.Equal((uint) (port ?? 80), tunnel.Port);
    }

    [Fact]
    public async Task Send_Request_To_Remote_Server_By_Http()
    {
        // Arrange
        using var remote = CreateServer(8761);
        var payload = "GET / HTTP/1.1\r\nHost: 127.0.0.1:8761\r\nUser-Agent: proxye-test\r\n\r\n";
        var data = Encoding.ASCII.GetBytes(payload);
        using var tunnel = new ProxyeTunnel(ServerSocket, new ProxyeOptions());
        var bytes = new byte[65535];

        // Act
        await ClientSocket.SendAsync(data);                             // Send request to proxy
        await tunnel.StartAsync(CancellationToken.None);                   // Receive and pass to remote
        var remoteServerSocket = await remote.GetServerSocket();           // Get remote socket
        var count = await remoteServerSocket.ReceiveAsync(bytes);       // Read remote socket
        var result = new string(bytes.Take(count).Select(x => (char)x).ToArray());

        // Assert
        Assert.Equal(payload, result);
    }

    [Fact]
    public async Task Send_Request_To_Remote_Server_By_Https()
    {
        // Arrange
        using var remote = CreateServer(8761);
        var shake = "CONNECT 127.0.0.1:9765 HTTP/1.1\r\nHost: 127.0.0.1:8761\r\n\r\n";
        var payload = "GET / HTTP/1.1\r\nHost: 127.0.0.1:8761\r\nUser-Agent: proxye-test\r\n\r\n";
        using var tunnel = new ProxyeTunnel(ServerSocket, new ProxyeOptions());

        // Act
        await ClientSocket.SendAsync(Encoding.ASCII.GetBytes(shake));       // Send request to proxy
        var tunnelTask = Task.Run(() => tunnel.StartAsync(CancellationToken.None));   // Tunnel should process HTTPS
        var clientTask = Task.Run(async () =>                                         // Client should send request 
        {
            await ClientSocket.ReceiveAsync(Bytes);
            await ClientSocket.SendAsync(Encoding.ASCII.GetBytes(payload));
        });
        await Task.WhenAll(tunnelTask, clientTask);
        var remoteServerSocket = await remote.GetServerSocket();                     // Get remote socket
        var count = await remoteServerSocket.ReceiveAsync(Bytes);                 // Read remote socket
        var result = new string(Bytes.Take(count).Select(x => (char)x).ToArray());

        // Assert
        Assert.Equal(payload, result);
    }

    [Fact]
    public async Task Pass_Data_Through()
    {
        // Arrange
        using var remote = CreateServer(8761);
        var payload = "GET / HTTP/1.1\r\nHost: 127.0.0.1:8761\r\nUser-Agent: proxye-test\r\n\r\n";
        var sampleData = "bla bla bla";
        using var tunnel = new ProxyeTunnel(ServerSocket, new ProxyeOptions());
        
        string? result = null;

        // Act
        await ClientSocket.SendAsync(Encoding.ASCII.GetBytes(payload));    // Send request to proxy
        await tunnel.StartAsync(CancellationToken.None);                             // Receive and pass to remote
        var remoteServerSocket = await remote.GetServerSocket();                     // Get remote socket
        var tunnelTask = Task.Run(() => tunnel.LoopAsync(CancellationToken.None));   // Tunneling data from client to remote and back
        var remoteTask = Task.Run(() => remoteServerSocket.SendAsync(Encoding.ASCII.GetBytes(sampleData))); // Send from remote some data
        var clientTask = Task.Run(async () =>
        {
            var count = await ClientSocket.ReceiveAsync(Bytes);                   // Receive some data by client
            result = new string(Bytes.Take(count).Select(x => (char)x).ToArray());
            remoteServerSocket.Close();                                              // And close connection
        });
        await Task.WhenAll(tunnelTask, remoteTask, clientTask);

        // Assert
        Assert.Equal(sampleData, result);
    }
}