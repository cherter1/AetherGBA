using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using AetherGba.Core.Bios;
using AetherGba.Core.Video.Ppu;
using GbaCartridge = AetherGba.Core.Cartridge.Cartridge;

namespace AetherGba.Core.Memory;

public sealed class GbaBus(GbaMemory memory, Ppu ppu)
{
    public unsafe void LoadBios(BiosImage? bios)
    {
        if (bios is not null)
        {
            fixed (byte* b = bios.Bytes)
            {
                NativeMemory.Copy(b, memory.Bios, (nuint)Math.Min(0x4000, bios.Bytes.Length));
            }
            //Array.Copy(bios.Bytes, memory.Bios, Math.Min(memory.Bios.Length, bios.Bytes.Length));
        }
    }

    public void FlushSave()
    {
        memory.Flash.FlushSave();
    }

    private const int MaxRomSize = 67108864;
    public unsafe void LoadCartridge(GbaCartridge? cartridge)
    {
        var length = cartridge?.RomData.Length ?? 0;
        length = Math.Min(length, MaxRomSize);
        memory.Rom = (byte*)NativeMemory.Alloc((nuint)length);
        memory.RomLen = length;

        fixed (byte* p = cartridge?.RomData)
        {
            NativeMemory.Copy(p, memory.Rom, (nuint)length);
        }
    }

    public unsafe uint Read32(uint address)
    {
        var aligned = address & 0xfffffffc;
        var reg = Regions[aligned >> 24];

        uint val = 0;
        switch (reg)
        {
            case MemoryRegion.Bios:
                //var off = (int)(aligned & 0x3fffu);
                //var buff = memory.Bios.AsSpan(off, 4);
                //val = BinaryPrimitives.ReadUInt32LittleEndian(buff);
                val = *(uint*)(memory.Bios + (aligned & 0x3fff));
                break;
            case MemoryRegion.Ewram:
                val = *(uint*)(memory.Ewram + (aligned & 0x3ffff));
                break;
            case MemoryRegion.Iwram:
                val = *(uint*)(memory.Iwram + (aligned & 0x7fff));
                break;
            case MemoryRegion.Io:
                val = memory.Io.ReadIo32Aligned(aligned);
                break;
            case MemoryRegion.PaletteRam:
                val = *(uint*)(memory.PaletteRam + (aligned & 0x3ff));
                break;
            case MemoryRegion.Vram:
                var offset = (aligned - 0x06000000) % 0x18000;
                val = *(uint*)(memory.Vram + offset);
                break;
            case MemoryRegion.Oam:
                val = ppu.ReadOam32Aligned(aligned);
                break;
            case MemoryRegion.Rom:
            case MemoryRegion.RomMirror:
            case MemoryRegion.RomMirrorTwo:
                uint romLen = (uint)memory.RomLen;
                uint roffset = aligned - 0x08000000u;
                if (roffset >= romLen)
                {
                    roffset = romLen == 0 ? 0 : roffset % romLen;
                }
                offset = roffset;
                //change later could still break if length  0
                val = *(uint*)(memory.Rom + (int)offset);
                break;
            case MemoryRegion.Sram:
            case MemoryRegion.Unused:
            default:
                break;
        }

        return aligned != address
            ? BitOperations.RotateRight(val, (int)((address & 3u) * 8))
            : val;
    }

    public unsafe ushort Read16(uint address)
    {
        address &= ~1u;
        var reg = Regions[address >> 24];

        ushort val = 0;
        switch (reg)
        {
            case MemoryRegion.Bios:
                //var off = (int)(address & 0x3fffu);
                //var buff = memory.Bios.AsSpan(off, 2);
                //val = (ushort)((buff[1] << 8) | buff[0]);
                val = *(ushort*)(memory.Bios + (address & 0x3fff));
                break;
            case MemoryRegion.Ewram:
                val = *(ushort*)(memory.Ewram + (address & 0x3ffff));
                break;
            case MemoryRegion.Iwram:
                val = *(ushort*)(memory.Iwram + (address & 0x7fff));
                break;
            case MemoryRegion.Io:
                val = memory.Io.ReadIo16Aligned(address);
                break;
            case MemoryRegion.PaletteRam:
                val = *(ushort*)(memory.PaletteRam + (address & 0x3ff));
                break;
            case MemoryRegion.Vram:
                var offset = (address - 0x06000000) % 0x18000;
                val = *(ushort*)(memory.Vram + offset);
                break;
            case MemoryRegion.Oam:
                val = ppu.ReadOam16Aligned(address);
                break;
            case MemoryRegion.Rom:
            case MemoryRegion.RomMirror:
            case MemoryRegion.RomMirrorTwo:
                uint romLen = (uint)memory.RomLen;
                uint roffset = address - 0x08000000u;
                if (roffset >= romLen)
                {
                    roffset = romLen == 0 ? 0 : roffset % romLen;
                }
                offset = roffset;
                val = *(ushort*)(memory.Rom + (int)offset);
                break;
            case MemoryRegion.Sram:
            case MemoryRegion.Unused:
            default:
                break;
        }

        return val;
    }

    public unsafe ushort ReadThumbInstruction(uint address)
    {
        var aligned = address & ~1u;
        var gamePakOffset = unchecked(aligned - 0x08000000u);

        // less than 32 Mib(max ROM size) offset for any of the 3 ROM replications then take fast path
        if (gamePakOffset >= 0x06000000u)
        {
            return Read16(address);
        }

        int offset = (int)(gamePakOffset & (memory.RomLen - 1)); // mod by max rom size for offset
        return *(ushort*)(memory.Rom + offset);
    }

    public unsafe byte Read8(uint address)
    {
        var reg = Regions[address >> 24];
        byte val;
        uint offset;
        switch (reg)
        {
            case MemoryRegion.Bios:
                val = memory.Bios[address & 0x3fffu];
                break;
            case MemoryRegion.Ewram:
                val = *(memory.Ewram + (address & 0x3ffffu));
                break;
            case MemoryRegion.Iwram:
                val = *(memory.Iwram + (address & 0x7fffu));
                break;
            case MemoryRegion.Io:
                val = memory.Io.ReadIo8(address);
                break;
            case MemoryRegion.PaletteRam:
                val = *(memory.PaletteRam + (address & 0x3ffu));
                break;
            case MemoryRegion.Vram:
                offset = (address - 0x06000000) % 0x18000;
                val = *(memory.Vram + offset);
                break;
            case MemoryRegion.Oam:
                val = ppu.ReadOam8(address);
                break;
            case MemoryRegion.Rom:
            case MemoryRegion.RomMirror:
            case MemoryRegion.RomMirrorTwo:
                uint romLen = (uint)memory.RomLen;
                uint roffset = address - 0x08000000u;
                if (roffset >= romLen)
                {
                    roffset = romLen == 0 ? 0 : roffset % romLen;
                }
                offset = roffset;
                val = *(memory.Rom + (int)offset);
                break;
            case MemoryRegion.Sram:
                val = memory.Flash.Read8(address); //modify for other saves later
                break;
            case MemoryRegion.Unused:
            default:
                val = 0;
                break;
        }
        return val;
    }

    public unsafe void Write32(uint address, uint value)
    {
        address &= 0xfffffffc;
        var reg = Regions[address >> 24];

        uint offset;
        switch (reg)
        {
            case MemoryRegion.Ewram:
                offset = address & 0x3ffff;
                *(uint*)(memory.Ewram + offset) = value;
                break;
            case MemoryRegion.Iwram:
                offset = address & 0x7fff;
                *(uint*)(memory.Iwram + offset) = value;
                break;
            case MemoryRegion.Io:
                memory.Io.WriteIo32Aligned(address, value);
                break;
            case MemoryRegion.PaletteRam:
                offset = address & 0x3ff;
                *(uint*)(memory.PaletteRam + offset) = value;
                break;
            case MemoryRegion.Vram:
                offset = (address - 0x06000000) % 0x18000;
                *(uint*)(memory.Vram + offset) = value;
                break;
            case MemoryRegion.Oam:
                ppu.WriteOam32Aligned(address, value);
                break;
            case MemoryRegion.Sram:
                break; //strange behavior
        }
    }

    public unsafe void Write16(uint address, ushort value)
    {
        address &= ~1u;
        var reg = Regions[address >> 24];

        uint offset;
        switch (reg)
        {
            case MemoryRegion.Ewram:
                offset = address & 0x3ffff;
                *(ushort*)(memory.Ewram + offset) = value;
                break;
            case MemoryRegion.Iwram:
                offset = address & 0x7fff;
                *(ushort*)(memory.Iwram + offset) = value;
                break;
            case MemoryRegion.Io:
                memory.Io.WriteIo16Aligned(address, value);
                break;
            case MemoryRegion.PaletteRam:
                offset = address & 0x3ff;
                *(ushort*)(memory.PaletteRam + offset) = value;
                break;
            case MemoryRegion.Vram:
                offset = (address - 0x06000000) % 0x18000;
                *(ushort*)(memory.Vram + offset) = value;
                break;
            case MemoryRegion.Oam:
                ppu.WriteOam16Aligned(address, value);
                break;
            case MemoryRegion.Sram:
                break; //Strange behavior
        }
    }

    public unsafe void Write8(uint address, byte value)
    {
        var reg = Regions[address >> 24];

        switch (reg)
        {
            case MemoryRegion.Ewram:
                *(memory.Ewram + (address & 0x3ffff)) = value;
                break;
            case MemoryRegion.Iwram:
                *(memory.Iwram + (address & 0x7fff)) = value;
                break;
            case MemoryRegion.Io:
                memory.Io.WriteIo8(address, value);
                break;
            case MemoryRegion.PaletteRam:
                var offset = address & 0x3feu; //mod by length -1 to halfword align
                *(ushort*)(memory.PaletteRam + offset) = (ushort)(value * 0x101);
                break;
            case MemoryRegion.Vram:
                var offs = (address - 0x06000000) % 0x18000;
                *(memory.Vram + offs) = value;
                //memory.Vram[offs + 1] = value;
                break; //strange behavior
            case MemoryRegion.Sram:
                memory.Flash.Write8(address, value); //mess with later or other save types
                break;
        }
    }

    private static readonly MemoryRegion[] Regions = new MemoryRegion[256];
    public static void InitMemoryRegions()
    {
        for (int i = 0; i < Regions.Length; i++)
        {
            Regions[i] = i switch
            {
                0x00 => MemoryRegion.Bios,
                0x02 => MemoryRegion.Ewram,
                0x03 => MemoryRegion.Iwram,
                0x04 => MemoryRegion.Io,
                0x05 => MemoryRegion.PaletteRam,
                0x06 => MemoryRegion.Vram,
                0x07 => MemoryRegion.Oam,
                0x08 or 0x09 => MemoryRegion.Rom,
                0x0a or 0x0b => MemoryRegion.RomMirror,
                0x0c or 0x0d => MemoryRegion.RomMirrorTwo,
                0x0e => MemoryRegion.Sram,
                _ => MemoryRegion.Unused
            };
        }
    }

    private static readonly int[] AccessCycles = new int[256 * 6];
    public static void InitAccessCycles()
    {
        for (int region = 0; region < 256; region++)
        {
            uint address = (uint)region << 24;
            int start = region * 6;

            AccessCycles[start] = CalculateCpuAccessCycles(address, AccessWidth.Byte, sequential: true, waitControl: 0);
            AccessCycles[start + 1] = CalculateCpuAccessCycles(address, AccessWidth.Halfword, sequential: true, waitControl: 0);
            AccessCycles[start + 2] = CalculateCpuAccessCycles(address, AccessWidth.Word, sequential: true, waitControl: 0);
            AccessCycles[start + 3] = CalculateCpuAccessCycles(address, AccessWidth.Byte, sequential: false, waitControl: 0);
            AccessCycles[start + 4] = CalculateCpuAccessCycles(address, AccessWidth.Halfword, sequential: false, waitControl: 0);
            AccessCycles[start + 5] = CalculateCpuAccessCycles(address, AccessWidth.Word, sequential: false, waitControl: 0);
        }
    }

    public static void HydrateAccessCycles(ushort waitControl)
    {
        for (int region = 8; region < 16; region++)
        {
            uint address = (uint)region << 24;
            int start = region * 6;

            AccessCycles[start] = CalculateCpuAccessCycles(address, AccessWidth.Byte, sequential: true, waitControl);
            AccessCycles[start + 1] = CalculateCpuAccessCycles(address, AccessWidth.Halfword, sequential: true, waitControl);
            AccessCycles[start + 2] = CalculateCpuAccessCycles(address, AccessWidth.Word, sequential: true, waitControl);
            AccessCycles[start + 3] = CalculateCpuAccessCycles(address, AccessWidth.Byte, sequential: false, waitControl);
            AccessCycles[start + 4] = CalculateCpuAccessCycles(address, AccessWidth.Halfword, sequential: false, waitControl);
            AccessCycles[start + 5] = CalculateCpuAccessCycles(address, AccessWidth.Word, sequential: false, waitControl);
        }
    }

    public int GetCpuAccessCycles(uint address, AccessWidth width, bool sequential)
    {
        int index = (int)(address >> 24) * 6 + (sequential ? 3 : 0) + (int)width;
        return AccessCycles[index];
    }
    private static int CalculateCpuAccessCycles(uint address, AccessWidth width, bool sequential, ushort waitControl)
    {
        return (int)(address >> 24) switch
        {
            0x00 => 1, //BIOS
            0x02 => width == AccessWidth.Word ? 6 : 3, //EWRAM
            0x03 => 1, //IWRAM
            0x04 => 1, //IO
            0x05 => width == AccessWidth.Word ? 2 : 1, //Palette RAM
            0x06 => width == AccessWidth.Word ? 2 : 1, //VRAM
            0x07 => 1, //OAM
            0x08 or 0x09 => GetGamePakRomCycles(waitState: 0, width, sequential, waitControl),
            0x0A or 0x0B => GetGamePakRomCycles(waitState: 1, width, sequential, waitControl),
            0x0C or 0x0D => GetGamePakRomCycles(waitState: 2, width, sequential, waitControl),
            0x0E or 0x0F => GetSramCycles(width, waitControl), //SRAM
            _ => 1
        };
    }

    private static int GetGamePakRomCycles(int waitState, AccessWidth width, bool sequential, ushort waitControl)
    {
        int first;
        int second;
        switch (waitState)
        {
            case 0:
                first = DecodeFirstAccess((waitControl >> 2) & 0b11);
                second = ((waitControl >> 4) & 1) == 0 ? 2 : 1;
                break;
            case 1:
                first = DecodeFirstAccess((waitControl >> 5) & 0b11);
                second = ((waitControl >> 7) & 1) == 0 ? 4 : 1;
                break;
            case 2:
                first = DecodeFirstAccess((waitControl >> 8) & 0b11);
                second = ((waitControl >> 10) & 1) == 0 ? 8 : 1;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(waitState));
        }

        int initial = sequential ? second : first;
        return width switch
        {
            AccessWidth.Byte => initial,
            AccessWidth.Halfword => initial,
            AccessWidth.Word => initial + second,
            _ => throw new ArgumentOutOfRangeException(nameof(width))
        };
    }

    private static int GetSramCycles(AccessWidth width, ushort waitControl)
    {
        var cycles = DecodeFirstAccess(waitControl & 0b11);
        return width switch
        {
            AccessWidth.Byte => cycles,
            AccessWidth.Halfword => cycles * 2,
            AccessWidth.Word => cycles * 4,
            _ => throw new ArgumentOutOfRangeException(nameof(width))
        };
    }

    private static int DecodeFirstAccess(int value)
    {
        return value switch
        {
            0 => 4,
            1 => 3,
            2 => 2,
            3 => 8,
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }
}