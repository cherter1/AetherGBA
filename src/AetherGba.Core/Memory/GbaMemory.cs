using System.Runtime.InteropServices;
using AetherGba.Core.Common;
using AetherGba.Core.Memory.SaveData;

namespace AetherGba.Core.Memory;

public sealed class GbaMemory
{
    public GbaMemory()
    {
        Flash = new FlashMemory("");
        Io = new IoRegisters(new GbaEventHandler());
    }

    public GbaMemory(string savePath, GbaEventHandler eventHandler)
    {
        Flash = new FlashMemory(savePath);
        Io = new IoRegisters(eventHandler);
    }

    //public readonly byte[] Bios = new byte[0x4000]; //16KB
    public readonly unsafe byte* Bios = (byte*)NativeMemory.AllocZeroed(0x4000);
    //public readonly byte[] Ewram = new byte[0x40000]; //256KB
    public readonly unsafe byte* Ewram = (byte*)NativeMemory.AllocZeroed(0x40000);
    //public readonly byte[] Iwram = new byte[0x8000]; //32KB
    public readonly unsafe byte* Iwram = (byte*)NativeMemory.AllocZeroed(0x8000);
    //public readonly byte[] PaletteRam = new byte[0x400]; //1KB
    public readonly unsafe byte* PaletteRam = (byte*)NativeMemory.AllocZeroed(0x400);
    //public readonly byte[] Vram = new byte[0x18000]; //96KB
    public readonly unsafe byte* Vram = (byte*)NativeMemory.AllocZeroed(0x18000);

    //public readonly byte[] Oam = new byte[0x400]; //1KB
    //public byte[] Rom = [];
    public unsafe byte* Rom;
    public int RomLen;
    public readonly byte[] Sram = new byte[0x10000]; //64KB
    public readonly FlashMemory Flash; //128KB
    public readonly IoRegisters Io;
}