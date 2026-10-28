using System.Runtime.InteropServices;

namespace AetherGba.App.SDL.Native.Structs;

[StructLayout(LayoutKind.Explicit, Size = 128)]
internal struct SdlKeyboardEvent
{
    [FieldOffset(28)]
    internal uint KeyCode;
}