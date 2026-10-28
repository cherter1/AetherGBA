namespace AetherGba.Core.Video.Backgrounds;

public readonly struct TextBackgroundScanlineInfo
{
    public readonly int TileDataStartOffset;
    public readonly int TileMapStartOffset;
    public readonly int XTiles;
    public readonly int CurrentYTile;
    public readonly int PixelYInTile;
    public readonly bool IsSinglePalette;

    public TextBackgroundScanlineInfo(int y, ushort vofs, ushort bgControl)
    {
        var tileDataStartOffset = ((bgControl >> 2) & 0b11) * 0x4000; // + 0x0600000 for address
        var tileMapStartOffset = ((bgControl >> 8) & 0x1f) * 0x800; // + 0x0600000 for address
        var tileMapSize = (bgControl >> 14) & 0b11;
        BackgroundHelpers.GetTextBackgroundSizeTiles(tileMapSize, out var xTiles, out var yTiles);

        var bgYStartOffset = y + vofs;
        if (yTiles > 32 && ((bgYStartOffset >> 8) & 1) != 0) //startOffset greater than pixels per map or SE length across AND yTiles > 32, if its 32 mirror the single Y SE
        {
            tileMapStartOffset += xTiles > 32 ? 0x1000 : 0x800; //move startOffset to next SE(map) start offset, if xTiles long then add 2 maps if not then only add 1 map length
        }
        bgYStartOffset &= 0xff;

        var tileY = bgYStartOffset >> 3; // div 8 to count tiles from offset
        var pixelYInTile = bgYStartOffset & 7; // modulo 8 for pixel 0-7 on x axis

        TileDataStartOffset = tileDataStartOffset;
        TileMapStartOffset = tileMapStartOffset;
        XTiles = xTiles;
        CurrentYTile = tileY;
        PixelYInTile = pixelYInTile;
        IsSinglePalette = (bgControl & 0x80) != 0;
    }
}