using System.Net.Sockets;

namespace Proxye.Rules.Helpers;

public static class SocketHelpers
{
    public static bool IsConnected(this Socket socket)
    {
        try
        {
            return !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0);
        }
        catch (SocketException)
        {
            return false;
        }
    }
}