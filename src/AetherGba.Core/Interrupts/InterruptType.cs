namespace AetherGba.Core.Interrupts;

[Flags]
public enum InterruptType : ushort
{
    VBlank = 1 << 0,
    HBlank = 1 << 1,
    VCounter = 1 << 2,
    Timer0 = 1 << 3,
    Timer1 = 1 << 4,
    Timer2 = 1 << 5,
    Timer3 = 1 << 6,
    Dma0 = 1 << 8,
    Dma1 = 1 << 9,
    Dma2 = 1 << 10,
    Dma3 = 1 << 11,
    Keypad = 1 << 12
}