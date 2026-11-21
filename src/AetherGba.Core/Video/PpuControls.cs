using System.Runtime.InteropServices;
using AetherGba.Core.Video.Sprites;

namespace AetherGba.Core.Video;

public sealed partial class Ppu
{
    private SpriteInfo[] _spriteInfo = new SpriteInfo[128];
    private readonly unsafe byte* _oam = (byte*)Marshal.AllocHGlobal(0x400).ToPointer(); //can only write 16/32

    public void WriteOam16Aligned(uint address, ushort value)
    {
        var rawOffset = address & 0x3ff;
        unsafe
        {
            *(ushort*)(_oam + rawOffset) = value;
        }
        var spriteIndex = rawOffset >> 3;
        var attrNum = (rawOffset & 7) >> 1;

        if (attrNum == 3) return; //TODO: handle affine info for multiple sprites

        ref var sprite = ref _spriteInfo[spriteIndex];
    }
}