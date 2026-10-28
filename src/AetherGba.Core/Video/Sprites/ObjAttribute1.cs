using AetherGba.Core.Common;

namespace AetherGba.Core.Video.Sprites;

public readonly struct ObjAttribute1
{
    public int XCoord { get; init; }

    //depends on attr0 rotatescaling
    public int RsParams { get; init; }

    public bool HorizontalMirrored { get; init; }

    public bool VerticalMirrored { get; init; }

    //TODO: Maybe make enum
    //depends on shape from attr0
    public uint ObjSize { get; init; }

    public ObjAttribute1(ushort attribute)
    {
        XCoord = attribute & 0x1ff;
        RsParams = (attribute >> 9) & 0x1f;
        HorizontalMirrored = BitUtils.IsBitSet(attribute, 12);
        VerticalMirrored = BitUtils.IsBitSet(attribute, 13);
        ObjSize = (uint)attribute >> 14;
    }
}