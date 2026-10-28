namespace AetherGba.Core.Video.SpecialEffects;

public enum BlendTargetTwoType : ushort
{
    Bg0 = 1 << 8,
    Bg1 = 1 << 9,
    Bg2 = 1 << 10,
    Bg3 = 1 << 11,
    Obj = 1 << 12,
    Backdrop = 1 << 13
}