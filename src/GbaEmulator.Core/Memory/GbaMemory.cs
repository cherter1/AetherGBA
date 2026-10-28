using GbaEmulator.Core.Memory.SaveData;

namespace GbaEmulator.Core.Memory;

public sealed class GbaMemory
{
    public readonly byte[] Bios = new byte[0x4000]; //16KB
    public readonly byte[] Ewram = new byte[0x40000]; //256KB
    public readonly byte[] Iwram = new byte[0x8000]; //32KB
    public readonly byte[] PaletteRam = new byte[0x400]; //1KB
    public readonly byte[] Vram = new byte[0x18000]; //96KB
    public readonly byte[] Oam = new byte[0x400]; //1KB
    public byte[] Rom = [];
    public readonly byte[] Sram = new byte[0x10000]; //64KB
    public readonly FlashMemory Flash = new(""); //128KB
    public readonly IoRegisters Io = new();
}