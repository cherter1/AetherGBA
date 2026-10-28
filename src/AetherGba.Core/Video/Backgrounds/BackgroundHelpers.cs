using System.Runtime.CompilerServices;

namespace AetherGba.Core.Video.Backgrounds;

public static class BackgroundHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetTextBackgroundSizeTiles(int size, out int numXTiles, out int numYTiles)
    {
        if (size == 0)
        {
            numXTiles = 32;
            numYTiles = 32;
        }
        else if (size == 0b01)
        {
            numXTiles = 64;
            numYTiles = 32;
        }
        else if (size == 0b10)
        {
            numXTiles = 32;
            numYTiles = 64;
        }
        else if (size == 0b11)
        {
            numXTiles = 64;
            numYTiles = 64;
        }
        else
        {
            numXTiles = 0;
            numYTiles = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetRotationalBackgroundSizePixels(int size)
    {
        return size switch
        {
            0 => 128,
            0b01 => 256,
            0b10 => 512,
            0b11 => 1024,
            _ => 0
        };
    }
}