using System.Runtime.InteropServices;

namespace AetherGba.Core.Video.Sprites;

[StructLayout(LayoutKind.Explicit)]
public struct ScanlineSpriteInfo
{
    [FieldOffset(0)] public int ScanlineStartMapTileNumber; //reg
    [FieldOffset(4)] public int YPixelOffset; //reg

    [FieldOffset(0)] public int TileNumber; //aff
    [FieldOffset(4)] public int RelativeY; //aff

    [FieldOffset(8)] public short XCoord; //9-bit sign-extend (-256 to 255)

    [FieldOffset(10)] public byte Priority;
    [FieldOffset(11)] public byte Mode;
    [FieldOffset(12)] public byte XTiles;
    [FieldOffset(13)] public byte PaletteNumber;
    [FieldOffset(14)] public bool IsAffine;
    [FieldOffset(15)] public bool SinglePalette;


    [FieldOffset(16)] public bool HFlip; //reg
    [FieldOffset(16)] public bool DoubleSize; //aff

    [FieldOffset(17)] public byte YTiles;
    [FieldOffset(18)] public short Pa;
    [FieldOffset(20)] public short Pb;
    [FieldOffset(22)] public short Pc;
    [FieldOffset(24)] public short Pd;

    public ScanlineSpriteInfo(
        int tileNumber,
        bool singlePalette,
        int paletteNumber,
        int yPixelOffset,
        int priority,
        int xTiles,
        int xCoord,
        int mode,
        bool hFlipOrDoubleSize,
        bool isAffine,
        short pa, short pb, short pc, short pd,
        int yTiles)
    {
        TileNumber = tileNumber;
        RelativeY = yPixelOffset;

        int rawX = xCoord & 0x1ff;
        XCoord = (short)((rawX << 23) >> 23); //sign-extend 9 bit for X wrapping i.e. 508 becomes -4

        Priority = (byte)priority;
        Mode = (byte)mode;
        XTiles = (byte)xTiles;
        PaletteNumber = (byte)paletteNumber;
        IsAffine = isAffine;
        SinglePalette = singlePalette;

        HFlip = hFlipOrDoubleSize;

        if (isAffine)
        {
            YTiles = (byte)yTiles;
            Pa = pa;
            Pb = pb;
            Pc = pc;
            Pd = pd;
        }
        else
        {
            YTiles = 0;
            Pa = 0;
            Pb = 0;
            Pc = 0;
            Pd = 0;
        }
    }
}