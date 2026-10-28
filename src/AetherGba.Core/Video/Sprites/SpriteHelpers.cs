using System.Runtime.CompilerServices;

namespace AetherGba.Core.Video.Sprites;

public static class SpriteHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetSpriteSizeTiles(uint shapeSize, out int xTiles, out int yTiles)
    {
        switch (shapeSize)
        {
            case 0b00_00: //8x8 pixels
                xTiles = 1;
                yTiles = 1;
                break;
            case 0b00_01: //16x16 pixels
                xTiles = 2;
                yTiles = 2;
                break;
            case 0b00_10: //32x32 pixels
                xTiles = 4;
                yTiles = 4;
                break;
            case 0b00_11: //64x64 pixels
                xTiles = 8;
                yTiles = 8;
                break;
            case 0b01_00: //16x8 pixels
                xTiles = 2;
                yTiles = 1;
                break;
            case 0b01_01: //32x8 pixels
                xTiles = 4;
                yTiles = 1;
                break;
            case 0b01_10: //32x16 pixels
                xTiles = 4;
                yTiles = 2;
                break;
            case 0b01_11: //64x32 pixels
                xTiles = 8;
                yTiles = 4;
                break;
            case 0b10_00: //8x16 pixels
                xTiles = 1;
                yTiles = 2;
                break;
            case 0b10_01: //8x32 pixels
                xTiles = 1;
                yTiles = 4;
                break;
            case 0b10_10: //16x32 pixels
                xTiles = 2;
                yTiles = 4;
                break;
            case 0b10_11: //32x64 pixels
                xTiles = 4;
                yTiles = 8;
                break;
            default:
                xTiles = 0;
                yTiles = 0;
                break;
        }
    }
}