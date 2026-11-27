using System.Runtime.InteropServices;

namespace AetherGba.Core.Video.Sprites;

[StructLayout(LayoutKind.Explicit)]
public struct SpriteInfo
{
    [FieldOffset(0)] public short StartTileNumber; //reg aff
    [FieldOffset(2)] public short XCoord; //reg aff

    [FieldOffset(4)] public bool IsAffine;

    [FieldOffset(5)] public bool IsDisabled; //reg   \
    [FieldOffset(5)] public bool IsDoubleSize; //aff /

    [FieldOffset(6)] public bool Is8Bpp; //reg aff
    [FieldOffset(7)] public byte Priority; //reg aff
    [FieldOffset(8)] public byte PaletteNumber; //reg aff
    [FieldOffset(9)] public SpriteMode Mode; //reg aff
    [FieldOffset(10)] public byte ShapeSize; //reg aff
    [FieldOffset(11)] public byte XTiles; //reg aff
    [FieldOffset(12)] public byte YTiles; //reg aff
    [FieldOffset(13)] public byte YCoord; //reg aff
    [FieldOffset(14)] public bool VFlip; //reg
    [FieldOffset(15)] public bool HFlip; //reg
    [FieldOffset(16)] public byte AffineGroup; //aff
}