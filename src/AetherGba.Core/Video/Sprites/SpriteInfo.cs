namespace AetherGba.Core.Video.Sprites;

public struct SpriteInfo
{
    public bool IsAffine;

    public bool IsDisabled; //reg
    public bool IsDoubleSize; //aff

    public bool IsSinglePalette; //reg
    public byte Priority; //reg
    public byte PaletteNumber;
    public SpriteMode Mode;
    public byte ShapeSize;
    
    //cleanup later maybe
    public byte XTiles; //reg
    public byte XCoord; //reg
    public byte YTiles; //reg
    public byte YCoord; //reg
    public bool VFlip; //reg
    public bool HFlip;
    //div 2 if single palette \/
    public short StartTileNumber; //reg
}