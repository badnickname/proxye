using System.Runtime.InteropServices;

namespace Proxye.Core.Models;

[StructLayout(LayoutKind.Auto)]
public readonly record struct Host(string Address, ushort Port);
