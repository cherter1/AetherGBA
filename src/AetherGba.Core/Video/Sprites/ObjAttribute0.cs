using AetherGba.Core.Common;

namespace AetherGba.Core.Video.Sprites;

public readonly struct ObjAttribute0
{
    public int YCoord { get; init; }

    public bool IsRotationScaling { get; init; }

    public bool IsDoubleSize { get; init; }

    public bool IsDisabled { get; init; }

    //TODO: maybe make enum
    //0=normal, 1=semi-transparent, 2=obj window, 3=prohibited
    public int ObjMode { get; init; }

    public bool MosaicEnabled { get; init; }

    public bool IsSinglePalette { get; init; }

    //TODO: maybe make enum
    //0=square, 1=Horizontal, 2=vertical, 3=prohibited
    public uint ObjShape { get; init; }

    public ObjAttribute0(ushort attribute)
    {
        YCoord = attribute & 0xFF;
        IsRotationScaling = BitUtils.IsBitSet(attribute, 8);
        IsDoubleSize = BitUtils.IsBitSet(attribute, 9);
        IsDisabled = BitUtils.IsBitSet(attribute, 9);
        ObjMode = (attribute >> 10) & 0b11;
        MosaicEnabled = BitUtils.IsBitSet(attribute, 12);
        IsSinglePalette = BitUtils.IsBitSet(attribute, 13);
        ObjShape = (uint)(attribute >> 14) & 0b11;
    }
}