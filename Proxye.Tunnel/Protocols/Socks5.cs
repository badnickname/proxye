using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using Proxye.Dns;
using Proxye.Rules;
using Proxye.Rules.Helpers;
using Proxye.Rules.Models;
using Proxye.Tunnel.Models;

namespace Proxye.Tunnel.Protocols;

internal sealed class Socks5(IRules rules, IDnsResolver dns) : IProtocol
{
    private static readonly byte[] Socks5ConnectArray = [5, 1, 0];

    public async Task<TunnelConnection> HandshakeAsync(Memory<byte> received, TunnelContext context)
    {
        var localBuffer = context.LocalBuffer;
        var remoteBuffer = context.RemoteBuffer;
        var token = context.CancellationToken;
        var count = received.Length;
        var socket = context.Socket;
        var response = new TunnelConnection();
        localBuffer[1] = 0;
        await socket.SendAsync(localBuffer.AsMemory()[..2], token);
        count = await socket.ReceiveAsync(localBuffer, token);
        switch (localBuffer[3])
        {
            case 1:
                // ip
                response.Host = $"{localBuffer[4]}.{localBuffer[5]}.{localBuffer[6]}.{localBuffer[7]}";
                response.Port = ((uint)(localBuffer[8]) << 8) | (uint) (localBuffer[9] & 0xff);
                response.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await response.Socket.ConnectAsync(response.Host, (int) response.Port, token);
                break;
            case 3:
                // host
                response.Host = StringHelpers.Read(localBuffer.AsSpan().Slice(5, localBuffer[4]), out _);
                response.Port = (((uint)localBuffer[5 + localBuffer[4]]) << 8) | (uint) (localBuffer[6 + localBuffer[4]] & 0xff);

                var ruleSocks = rules.Match(response.Host);
                if (ruleSocks?.Protocol == Protocol.SOCKS5)
                {
                    response.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    await response.Socket.ConnectAsync(ruleSocks.Host, ruleSocks.Port, token);
                    await response.Socket.SendAsync(Socks5ConnectArray, token);
                    await response.Socket.ReceiveAsync(remoteBuffer, token); // todo: handle answer

                    remoteBuffer[0] = 5;
                    remoteBuffer[1] = 1;
                    remoteBuffer[2] = 0;
                    remoteBuffer[3] = 3;
                    remoteBuffer[4] = (byte) response.Host.Length;
                    for (var i = 0; i < response.Host.Length; i++)
                    {
                        remoteBuffer[5 + i] = (byte) response.Host[i];
                    }

                    remoteBuffer[5 + response.Host.Length] = (byte) (response.Port >> 8);
                    remoteBuffer[6 + response.Host.Length] = (byte) (response.Port & 0xff);
                    await response.Socket.SendAsync(remoteBuffer.AsMemory()[..(7 + response.Host.Length)], token);
                    await response.Socket.ReceiveAsync(remoteBuffer, token); // todo: handle answer
                }
                else
                {
                    var resolvedIp = await dns.Resolve(response.Host, token);
                    response.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    await response.Socket.ConnectAsync(resolvedIp, (int) response.Port, token);
                }
                break;
            case 4:
                // ipv6
                response.Host = $"{localBuffer[4]:X}{localBuffer[5]:X}:{localBuffer[6]:X}{localBuffer[7]:X}:{localBuffer[8]:X}{localBuffer[9]:X}:{localBuffer[10]:X}{localBuffer[11]:X}";
                response.Port = ((uint)localBuffer[12]) << 8 | (uint) (localBuffer[13] & 0xff);
                response.Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                await response.Socket.ConnectAsync(response.Host, (int) response.Port, token);
                break;
            default:
                throw new InvalidEnumArgumentException();
        }

        localBuffer[1] = 0;
        localBuffer[2] = 0;
        var ip = response.Socket.RemoteEndPoint as IPEndPoint;
        byte[] bytes;
        switch (ip?.AddressFamily)
        {
            case AddressFamily.InterNetwork:
                bytes = ip.Address.GetAddressBytes();
                localBuffer[3] = 1;
                localBuffer[4] = bytes[0];
                localBuffer[5] = bytes[1];
                localBuffer[6] = bytes[2];
                localBuffer[7] = bytes[3];
                localBuffer[8] = (byte) (ip.Port >> 8);
                localBuffer[9] = (byte) (ip.Port & 0xff);
                await socket.SendAsync(localBuffer.AsMemory()[..10], token);
                break;
            case AddressFamily.InterNetworkV6:
                bytes = ip.Address.GetAddressBytes();
                localBuffer[3] = 1;
                localBuffer[4] = bytes[0];
                localBuffer[5] = bytes[1];
                localBuffer[6] = bytes[2];
                localBuffer[7] = bytes[3];
                localBuffer[8] = bytes[4];
                localBuffer[9] = bytes[5];
                localBuffer[10] = bytes[6];
                localBuffer[11] = bytes[7];
                localBuffer[12] = (byte) (ip.Port >> 8);
                localBuffer[13] = (byte) (ip.Port & 0xff);
                await socket.SendAsync(localBuffer.AsMemory()[..14], token);
                break;
            default:
                await socket.SendAsync(localBuffer.AsMemory()[..count], token);
                break;
        }
        return response;
    }
}