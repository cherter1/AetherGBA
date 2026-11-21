namespace AetherGba.Core.Video.Sprites;

public struct SpriteInfo
{
    public bool IsAffine;

    public bool IsDisabled; //reg   \
    public bool IsDoubleSize; //aff /

    public bool Is8Bpp; //reg aff
    public byte Priority; //reg aff
    public byte PaletteNumber; //reg aff
    public SpriteMode Mode; //reg aff
    public byte ShapeSize; //reg aff
    
    //cleanup later maybe
    public byte XTiles; //reg aff
    public byte XCoord; //reg aff
    public byte YTiles; //reg aff
    public byte YCoord; //reg aff
    public bool VFlip; //reg
    public bool HFlip; //reg
    //div 2 if single palette \/
    public short StartTileNumber; //reg aff
    public byte AffineGroup; //aff
}