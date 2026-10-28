namespace AetherGba.Core.Video.SpecialEffects;

public static class SpecialEffectsHelper
{
    public static ushort AlphaBlendPixels(ushort t1Bgr555Color, ushort t2Bgr555Color, ushort blendAlphaReg)
    {
        float coEfA = (blendAlphaReg & 0x1f) / 16f;
        float coEfB = ((blendAlphaReg >> 8) & 0x1f) / 16f;

        var t1Red = t1Bgr555Color & 0x1f;
        var t1Green = (t1Bgr555Color >> 5) & 0x1f;
        var t1Blue = (t1Bgr555Color >> 10) & 0x1f;

        var t2Red = t2Bgr555Color & 0x1f;
        var t2Green = (t2Bgr555Color >> 5) & 0x1f;
        var t2Blue = (t2Bgr555Color >> 10) & 0x1f;

        //blend
        var red = Math.Min(31, (t1Red * coEfA) + (t2Red * coEfB));
        var green = Math.Min(31, (t1Green * coEfA) + (t2Green * coEfB));
        var blue = Math.Min(31, (t1Blue * coEfA) + (t2Blue * coEfB));

        return (ushort)((uint)red | ((uint)green << 5) | ((uint)blue << 10));
    }

    public static ushort LightenBlend(ushort t1Bgr555Color, ushort blendYReg)
    {
        var coEf = (blendYReg & 0x1f) / 16f;

        var t1Red = t1Bgr555Color & 0x1f;
        var t1Green = (t1Bgr555Color >> 5) & 0x1f;
        var t1Blue = (t1Bgr555Color >> 10) & 0x1f;

        var redInc = t1Red + (31 - t1Red) * coEf;
        var blueInc = t1Blue + (31 - t1Blue) * coEf;
        var greenInc = t1Green + (31 - t1Green) * coEf;

        return (ushort)((uint)redInc | ((uint)greenInc << 5) | ((uint)blueInc << 10));
    }

    public static ushort DarkenBlend(ushort t1Bgr555Color, ushort blendYReg)
    {
        var coEf = (blendYReg & 0x1f) / 16f;

        var t1Red = t1Bgr555Color & 0x1f;
        var t1Green = (t1Bgr555Color >> 5) & 0x1f;
        var t1Blue = (t1Bgr555Color >> 10) & 0x1f;

        var redDec = t1Red - t1Red * coEf;
        var blueDec = t1Blue - t1Blue * coEf;
        var greenDec = t1Green - t1Green * coEf;

        return (ushort)((uint)redDec | ((uint)greenDec << 5) | ((uint)blueDec << 10));
    }

    public static bool TryGetWindowRange(int windowNum, int y, ushort displayControl, ushort winH, ushort winV, out byte winStartX, out uint winXThreshold)
    {
        int windowEnabledMask = windowNum == 0 ? 0x2000 : 0x4000;
        if ((displayControl & windowEnabledMask) == 0)
        {
            winStartX = 0;
            winXThreshold = 0;
            return false;
        }

        byte winEndX = (byte)(winH & 0xff); //bits 0-7 x2 rightMost
        winStartX = (byte)(winH >> 8); //bits 8-15 x1 leftMost

        winXThreshold = (uint)(winEndX - winStartX);

        byte winEndY = (byte)(winV & 0xff); //bits 0-7 y2 bottomMost
        byte winStartY = (byte)(winV >> 8); //bits 8-15 y1 topMost

        return (uint)(y - winStartY) < (uint)(winEndY - winStartY);
    }
}