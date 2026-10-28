namespace AetherGba.Core.Video.SpecialEffects;

public static class BlendTargetTypeExtensions
{
    public static BlendTargetTwoType ToBlendTargetTwoType(this BlendTargetOneType t1)
        => (BlendTargetTwoType)((ushort)t1 << 8);
}