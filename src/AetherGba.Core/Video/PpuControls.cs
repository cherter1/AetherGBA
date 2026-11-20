using System.Runtime.InteropServices;

namespace AetherGba.Core.Video;

public sealed partial class Ppu
{
    //private readonly unsafe byte* _oam = (byte*)Marshal.AllocHGlobal(0x400).ToPointer();

    public void WriteOam16(uint address)
    {
        var rawOffset = address & 0x3ff;
    }
}