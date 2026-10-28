namespace AetherGba.Core.Dma;

public enum DmaTimingType
{
    Immediately = 0,
    VBlank = 1,
    Hblank = 1 << 1,
    Special = 3
}