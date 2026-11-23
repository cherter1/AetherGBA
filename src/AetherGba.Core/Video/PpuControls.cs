using System.Runtime.InteropServices;
using AetherGba.Core.Video.Sprites;

namespace AetherGba.Core.Video;

public sealed partial class Ppu
{
    private AffineParams[] _affineParameters = new AffineParams[32];
    private SpriteInfo[] _spriteInfo = new SpriteInfo[128];
    private readonly unsafe byte* _oam = (byte*)Marshal.AllocHGlobal(0x400).ToPointer(); //can only write 16/32

    public void WriteOam16Aligned(uint address, ushort value)
    {
        var rawOffset = address & 0x3ff;
        unsafe
        {
            *(ushort*)(_oam + rawOffset) = value;
        }
        var attrNum = (rawOffset & 7) >> 1;

        if (attrNum == 3)
        {
            UpdateAffineParams(rawOffset, (short)value);
            return;
        }

        var spriteIndex = rawOffset >> 3;
        ref var sprite = ref _spriteInfo[spriteIndex];
    }

    private void UpdateAttr0(ref SpriteInfo spriteInfo, ushort value)
    {
        spriteInfo.Is8Bpp = (value & 0x4000) != 0;
        spriteInfo.YCoord = (byte)(value & 0xFF);
        spriteInfo.IsAffine = (value & 0x100) != 0;
        if (spriteInfo.IsAffine)
        {
            spriteInfo.IsDoubleSize = (value & 0x200) != 0;
        }
        else
        {
            spriteInfo.IsDisabled = (value & 0x200) != 0;
        }

        spriteInfo.Mode = (SpriteMode)((value >> 10) & 0b11);
        spriteInfo.ShapeSize = (byte)((((value >> 14) & 0b11) << 2) | (spriteInfo.ShapeSize & 0b11));

        SpriteHelpers.GetSpriteSizeTiles(spriteInfo.ShapeSize, out int xTiles, out int yTiles);
        spriteInfo.XTiles = (byte)xTiles;
        spriteInfo.YTiles = (byte)yTiles;
    }

    private void UpdateAttr1(ref SpriteInfo spriteInfo, ushort value)
    {
        spriteInfo.XCoord = value & 0x1ff;
        spriteInfo.AffineGroup = (byte)((value >> 9) & 0x1f);
        spriteInfo.HFlip = (value & 0x1000) != 0;
        spriteInfo.VFlip = (value & 0x2000) != 0;

        spriteInfo.ShapeSize = (byte)((spriteInfo.ShapeSize & 0b1100) | ((value >> 14) & 0b11));
        SpriteHelpers.GetSpriteSizeTiles(spriteInfo.ShapeSize, out int xTiles, out int yTiles);
        spriteInfo.XTiles = (byte)xTiles;
        spriteInfo.YTiles = (byte)yTiles;
    }

    private void UpdateAttr2(ref SpriteInfo spriteInfo, ushort value)
    {
        spriteInfo.StartTileNumber = (short)(value & 0x3ff);
        spriteInfo.Priority = (byte)((value >> 10) & 0b11);
        spriteInfo.PaletteNumber = (byte)((value >> 12) & 0xf);
    }

    private void UpdateAffineParams(uint rawOffset, short value)
    {
        var paramIdx = rawOffset >> 5;
        ref var affineParams = ref _affineParameters[paramIdx];

        var paramPart = (rawOffset & 31) >> 3;
        switch (paramPart)
        {
            case 0: affineParams.Pa = value; break;
            case 1: affineParams.Pb = value; break;
            case 2: affineParams.Pc = value; break;
            case 3: affineParams.Pd = value; break;
        }
    }
}