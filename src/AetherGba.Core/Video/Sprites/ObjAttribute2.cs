namespace AetherGba.Core.Video.Sprites;

public readonly struct ObjAttribute2
{
    public int TileNumber { get; init; }

    public int Priority { get; init; }

    public int PaletteNumber { get; init; }

    public ObjAttribute2(ushort attribute)
    {
        TileNumber = attribute & 0x3ff;
        Priority = (attribute >> 10) & 0b11;
        PaletteNumber = (attribute >> 12) & 0xf;
    }
}