using System.Text;
using Proxye.Test.Utils;

namespace Proxye.Test;

public class TunnelTest
{
    [Theory]
    [InlineData("127.0.0.1", 443)]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.1", 123)]
    public async Task Read_Host_And_Port(string host, int? port = null)
    {
        // Arrange
        using var testServer = new TestServer(9765);
        using var remoteServer = new TestServer(port ?? 80);
        remoteServer.Start();
        testServer.Start();
        var client = await testServer.GetClientSocket();
        var server = await testServer.GetServerSocket();
        var hostname = port.HasValue ? $"{host}:{port.Value}" : host;
        var data = Encoding.ASCII.GetBytes($"GET / HTTP/1.1\r\nHost: {hostname}\r\nUser-Agent: proxye-test\r\n\r\n");
        using var tunnel = new ProxyeTunnel(server, new ProxyeOptions());

        // Act
        await client.SendAsync(data);                   // Send request to proxy
        await tunnel.StartAsync(CancellationToken.None);   // Receive request and get Host and port

        // Assert
        Assert.Equal(host, tunnel.Host);
        Assert.Equal((uint) (port ?? 80), tunnel.Port);
    }

    [Fact]
    public async Task Send_Request_To_Remote_Server_By_Http()
    {
        // Arrange
        using var testServer = new TestServer(9765);
        using var remoteServer = new TestServer(8761);
        remoteServer.Start();
        testServer.Start();
        var client = await testServer.GetClientSocket();
        var server = await testServer.GetServerSocket();
        var payload = "GET / HTTP/1.1\r\nHost: 127.0.0.1:8761\r\nUser-Agent: proxye-test\r\n\r\n";
        var data = Encoding.ASCII.GetBytes(payload);
        using var tunnel = new ProxyeTunnel(server, new ProxyeOptions());
        var bytes = new byte[65535];

        // Act
        await client.SendAsync(data);                            // Send request to proxy
        await tunnel.StartAsync(CancellationToken.None);            // Receive and pass to remote
        var remote = await remoteServer.GetServerSocket();   // Get remote socket
        var count = await remote.ReceiveAsync(bytes);            // Read remote socket
        var result = new string(bytes.Take(count).Select(x => (char)x).ToArray());

        // Assert
        Assert.Equal(payload, result);
    }

    [Fact]
    public async Task Send_Request_To_Remote_Server_By_Https()
    {
        // Arrange
        using var testServer = new TestServer(9765);
        using var remoteServer = new TestServer(8761);
        remoteServer.Start();
        testServer.Start();
        var client = await testServer.GetClientSocket();
        var server = await testServer.GetServerSocket();
        var shake = "CONNECT 127.0.0.1:9765 HTTP/1.1\r\nHost: 127.0.0.1:8761\r\n\r\n";
        var payload = "GET / HTTP/1.1\r\nHost: 127.0.0.1:8761\r\nUser-Agent: proxye-test\r\n\r\n";
        using var tunnel = new ProxyeTunnel(server, new ProxyeOptions());
        var bytes = new byte[65535];

        // Act
        await client.SendAsync(Encoding.ASCII.GetBytes(shake));             // Send request to proxy
        var tunnelTask = Task.Run(() => tunnel.StartAsync(CancellationToken.None));   // Tunnel should process HTTPS
        var clientTask = Task.Run(async () =>                                         // Client should send request 
        {
            await client.ReceiveAsync(bytes);
            await client.SendAsync(Encoding.ASCII.GetBytes(payload));
        });
        await Task.WhenAll(tunnelTask, clientTask);
        var remote = await remoteServer.GetServerSocket();                     // Get remote socket
        var count = await remote.ReceiveAsync(bytes);                              // Read remote socket
        var result = new string(bytes.Take(count).Select(x => (char)x).ToArray());

        // Assert
        Assert.Equal(payload, result);
    }

    [Fact]
    public async Task Pass_Data_Through()
    {
        // Arrange
        using var testServer = new TestServer(9765);
        using var remoteServer = new TestServer(8761);
        remoteServer.Start();
        testServer.Start();
        var client = await testServer.GetClientSocket();
        var server = await testServer.GetServerSocket();
        var payload = "GET / HTTP/1.1\r\nHost: 127.0.0.1:8761\r\nUser-Agent: proxye-test\r\n\r\n";
        var sampleData = "bla bla bla";
        using var tunnel = new ProxyeTunnel(server, new ProxyeOptions());
        var bytes = new byte[65535];
        string? result = null;

        // Act
        await client.SendAsync(Encoding.ASCII.GetBytes(payload));    // Send request to proxy
        await tunnel.StartAsync(CancellationToken.None);                       // Receive and pass to remote
        var remote = await remoteServer.GetServerSocket();              // Get remote socket
        var tunnelTask = Task.Run(() => tunnel.LoopAsync(CancellationToken.None));  // Tunneling data from client to remote and back
        var remoteTask = Task.Run(() => remote.SendAsync(Encoding.ASCII.GetBytes(sampleData))); // Send from remote some data
        var clientTask = Task.Run(async () =>
        {
            var count = await client.ReceiveAsync(bytes);                   // Receive some data by client
            result = new string(bytes.Take(count).Select(x => (char)x).ToArray());
            remote.Close();                                                    // And close connection
        });
        await Task.WhenAll(tunnelTask, remoteTask, clientTask);

        // Assert
        Assert.Equal(sampleData, result);
    }
}