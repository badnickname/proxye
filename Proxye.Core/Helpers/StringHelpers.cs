using System.Text;

namespace Proxye.Core.Helpers;

public static class StringHelpers
{
    public static int GetStartOf(int count, Span<byte> buffer, int targetHash, Span<byte> target)
    {
        var hash = 0;
        for (var i = 0; i < target.Length; i++) hash += buffer[i];
        for (var i = 0; i < count - target.Length; i++)
        {
            if (hash == targetHash)
            {
                for (var j = 0; j < target.Length; j++)
                {
                    if (buffer[i + j] != target[j]) break;
                }
                return i;
            }

            hash -= buffer[i];
            hash += buffer[i + target.Length];
        }

        return -1;
    }

    public static string Read(Span<byte> buffer, out int lenght, char endChar = '\0')
    {
        var sb = new StringBuilder();
        lenght = 0;
        foreach (var t in buffer)
        {
            if (t != '\r' && t != '\n' && t != '\0' && t != endChar)
            {
                lenght++;
                sb.Append((char) t);
            }
            else
            {
                break;
            }
        }
        return sb.ToString();
    }
}