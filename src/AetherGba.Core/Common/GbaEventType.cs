namespace AetherGba.Core.Common;

public enum GbaEventType : byte
{
    Root = 0,
    EnterHBlank = 1,
    EndScanline = 2,
    DmaImmediate = 3,
    TimerOverflow = 4
}