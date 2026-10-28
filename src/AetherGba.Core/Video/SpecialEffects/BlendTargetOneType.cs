namespace AetherGba.Core.Video.SpecialEffects;

public enum BlendTargetOneType : ushort
{
    Bg0 = 1,
    Bg1 = 1 << 1,
    Bg2 = 1 << 2,
    Bg3 = 1 << 3,
    Obj = 1 << 4,
    Backdrop = 1 << 5
}