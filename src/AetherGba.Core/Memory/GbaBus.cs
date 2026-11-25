using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using AetherGba.Core.Bios;
using AetherGba.Core.Video.Ppu;
using GbaCartridge = AetherGba.Core.Cartridge.Cartridge;

namespace AetherGba.Core.Memory;

public sealed class GbaBus(GbaMemory memory, Ppu ppu)
{
    public void LoadBios(BiosImage? bios)
    {
        if (bios is not null)
        {
            Array.Copy(bios.Bytes, memory.Bios, Math.Min(memory.Bios.Length, bios.Bytes.Length));
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

    public uint Read32(uint address)
    {
        var aligned = address & 0xfffffffc;
        var region = ResolveRegion(aligned, 4, out var buffer);

        uint raw = region switch
        {
            MemoryRegion.Unused => 0,
            MemoryRegion.Io => memory.Io.ReadIo32Aligned(aligned),
            MemoryRegion.Oam => ppu.ReadOam32Aligned(aligned),
            _ => BinaryPrimitives.ReadUInt32LittleEndian(buffer)
        };

        return aligned != address
            ? BitOperations.RotateRight(raw, (int)((address & 3u) * 8))
            : raw;
    }

    public ushort Read16(uint address)
    {
        address &= ~1u;
        var region = ResolveRegion(address, 2, out var buffer);
        return region switch
        {
            MemoryRegion.Unused => 0,
            MemoryRegion.Io => memory.Io.ReadIo16Aligned(address),
            MemoryRegion.Oam => ppu.ReadOam16Aligned(address),
            _ => (ushort)((buffer[1] << 8) | buffer[0])
        };
    }

    public unsafe ushort ReadThumbInstruction(uint address)
    {
        var aligned = address & ~1u;
        var gamePakOffset = unchecked(aligned - 0x08000000u);

        // less than 32 Mib(max ROM size) offset for any of the 3 ROM replications then take fast path
        if (gamePakOffset >= 0x06000000u)
        {
            //Console.WriteLine("slow path");
            return Read16(address);
        }

        int offset = (int)(gamePakOffset & (memory.RomLen - 1)); // mod by max rom size for offset
        return *(ushort*)(memory.Rom + offset);
        return *(ushort*)memory.Rom[offset];
    }

    public unsafe byte Read8(uint address)
    {
        var reg = Regions[address >> 24];
        byte val;
        uint offset;
        switch (reg)
        {
            case MemoryRegion.Bios:
                offset = address & 0x3fff;
                val = memory.Bios[offset];
                break;
            case MemoryRegion.Ewram:
                offset = address & 0x3ffff;
                val = memory.Ewram[offset];
                break;
            case MemoryRegion.Iwram:
                offset = address & 0x7fff;
                val = memory.Iwram[offset];
                break;
            case MemoryRegion.Io:
                val = memory.Io.ReadIo8(address);
                break;
            case MemoryRegion.PaletteRam:
                offset = address & 0x3ff;
                val = memory.PaletteRam[offset];
                break;
            case MemoryRegion.Vram:
                offset = address & 0x17fff;
                val = memory.Vram[offset];
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
                val = *(memory.Rom + offset);
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

        /*
        var region = ResolveRegion(address, 1, out var buffer);
        return region switch
        {
            MemoryRegion.Unused => 0,
            MemoryRegion.Io => memory.Io.ReadIo8(address),
            MemoryRegion.Sram => memory.Flash.Read8(address),
            MemoryRegion.Oam => ppu.ReadOam8(address),
            MemoryRegion.Rom => memory.Rom[2],
            _ => buffer[0]
        };
        */
    }

    public void Write32(uint address, uint value)
    {
        address &= ~3u;
        var reg = Regions[address >> 24];

        uint offset;
        switch (reg)
        {
            case MemoryRegion.Ewram:
                offset = address & 0x3ffff;
                memory.Ewram[offset + 3] = (byte)(value >> 24);
                memory.Ewram[offset + 2] = (byte)(value >> 16);
                memory.Ewram[offset + 1] = (byte)(value >> 8);
                memory.Ewram[offset] = (byte)value;
                break;
            case MemoryRegion.Iwram:
                offset = address & 0x7fff;
                memory.Iwram[offset + 3] = (byte)(value >> 24);
                memory.Iwram[offset + 2] = (byte)(value >> 16);
                memory.Iwram[offset + 1] = (byte)(value >> 8);
                memory.Iwram[offset] = (byte)value;
                break;
            case MemoryRegion.Io:
                memory.Io.WriteIo32Aligned(address, value);
                break;
            case MemoryRegion.PaletteRam:
                offset = address & 0x3ff;
                memory.PaletteRam[offset + 3] = (byte)(value >> 24);
                memory.PaletteRam[offset + 2] = (byte)(value >> 16);
                memory.PaletteRam[offset + 1] = (byte)(value >> 8);
                memory.PaletteRam[offset] = (byte)value;
                break;
            case MemoryRegion.Vram:
                offset = address & 0x17fff;
                memory.Vram[offset + 3] = (byte)(value >> 24);
                memory.Vram[offset + 2] = (byte)(value >> 16);
                memory.Vram[offset + 1] = (byte)(value >> 8);
                memory.Vram[offset] = (byte)value;
                break;
            case MemoryRegion.Oam:
                ppu.WriteOam32Aligned(address, value);
                break;
            case MemoryRegion.Sram:
                break; //strange behavior
        }

        /*
        var region = ResolveRegion(address, 4, out var buffer);

        switch (region)
        {
            case MemoryRegion.Bios or MemoryRegion.Rom or MemoryRegion.Unused:
                return;
            case MemoryRegion.Io:
                memory.Io.WriteIo32Aligned(address, value);
                break;
            case MemoryRegion.Oam:
                ppu.WriteOam32Aligned(address, value);
                break;
            default:
                BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
                break;
        }
        */
    }

    public void Write16(uint address, ushort value)
    {
        address &= ~1u;
        var reg = Regions[address >> 24];

        uint offset;
        switch (reg)
        {
            case MemoryRegion.Ewram:
                offset = address & 0x3ffff;
                memory.Ewram[offset + 1] = (byte)(value >> 8);
                memory.Ewram[offset] = (byte)value;
                break;
            case MemoryRegion.Iwram:
                offset = address & 0x7fff;
                memory.Iwram[offset + 1] = (byte)(value >> 8);
                memory.Iwram[offset] = (byte)value;
                break;
            case MemoryRegion.Io:
                memory.Io.WriteIo16Aligned(address, value);
                break;
            case MemoryRegion.PaletteRam:
                offset = address & 0x3ff;
                memory.PaletteRam[offset + 1] = (byte)(value >> 8);
                memory.PaletteRam[offset] = (byte)value;
                break;
            case MemoryRegion.Vram:
                offset = address & 0x17fff;
                memory.Vram[offset + 1] = (byte)(value >> 8);
                memory.Vram[offset] = (byte)value;
                break;
            case MemoryRegion.Oam:
                ppu.WriteOam16Aligned(address, value);
                break;
            case MemoryRegion.Sram:
                break; //Strange behavior
        }

        /*
        var region = ResolveRegion(address, 2, out var buffer);
        switch (region)
        {
            case MemoryRegion.Bios or MemoryRegion.Rom or MemoryRegion.Unused:
                return;
            case MemoryRegion.Io:
                memory.Io.WriteIo16Aligned(address, value);
                break;
            case MemoryRegion.Oam:
                ppu.WriteOam16Aligned(address, value);
                break;
            default:
                buffer[1] = (byte)(value >> 8);
                buffer[0] = (byte)value;
                break;
        }
        */
    }

    public void Write8(uint address, byte value)
    {
        var reg = Regions[address >> 24];

        switch (reg)
        {
            case MemoryRegion.Ewram:
                memory.Ewram[address & 0x3ffff] = value;
                break;
            case MemoryRegion.Iwram:
                memory.Iwram[address & 0x7fff] = value;
                break;
            case MemoryRegion.Io:
                memory.Io.WriteIo8(address, value);
                break;
            case MemoryRegion.PaletteRam:
                var offset = address & 0x3ffe; //mod by length -1 to halfword align
                memory.PaletteRam[offset] = value;
                memory.PaletteRam[offset + 1] = value;
                break;
            case MemoryRegion.Vram:
                break; //strange behavior
            case MemoryRegion.Sram:
                memory.Flash.Write8(address, value); //mess with later or other save types
                break;
        }

        /*
        var region = ResolveRegion(address, 1, out var buffer);
        switch (region)
        {
            case MemoryRegion.Bios or MemoryRegion.Rom or MemoryRegion.Unused or MemoryRegion.Oam:
                return;
            case MemoryRegion.Io:
                memory.Io.WriteIo8(address, value);
                break;
            case MemoryRegion.Sram:
                memory.Flash.Write8(address, value);
                break;
            default:
                buffer[0] = value;
                break;
        }
        */
    }

    private unsafe MemoryRegion ResolveRegion(uint address, int size, out Span<byte> buffer)
    {
        int offset;
        switch (address >> 24)
        {
            case 0x00:
                offset = (int)(address & 0x3fffu); //fixed pow 2 size 16 KiB
                //offset = (int)(address % (uint)memory.Bios.Length);
                buffer = memory.Bios.AsSpan(offset, size);
                return MemoryRegion.Bios;
            case 0x02:
                offset = (int)(address & 0x3ffffu); //fixed pow 2 size 256 KiB
                //offset = (int)((address - 0x02000000) % (uint)memory.Ewram.Length);
                buffer = memory.Ewram.AsSpan(offset, size);
                return MemoryRegion.Ewram;
            case 0x03:
                offset = (int)(address & 0x7fffu); //fixed pow 2 size 16 KiB
                //offset = (int)((address - 0x03000000) % (uint)memory.Iwram.Length);
                buffer = memory.Iwram.AsSpan(offset, size);
                return MemoryRegion.Iwram;
            case 0x04:
                buffer = [];
                return MemoryRegion.Io;
            case 0x05:
                offset = (int)(address & 0x3ffu); //fixed pow 2 size 1 KiB
                //offset = (int)((address - 0x05000000) % (uint)memory.PaletteRam.Length);
                buffer = memory.PaletteRam.AsSpan(offset, size);
                return MemoryRegion.PaletteRam;
            case 0x06:
                offset = (int)((address - 0x06000000) % (uint)memory.Vram.Length);
                buffer = memory.Vram.AsSpan(offset, size);
                return MemoryRegion.Vram;
            case 0x07:
                //offset = (int)(address & 0x3ffu); //fixed pow 2 size 1 KiB
                //buffer = memory.Oam.AsSpan(offset, size);
                buffer = [];
                return MemoryRegion.Oam;
            case 0x08:
            case 0x09:
            case 0x0A:
            case 0x0B:
            case 0x0C:
            case 0x0D:
                
                uint romLen = (uint)memory.RomLen;
                uint roffset = address - 0x08000000u;
                if (roffset >= romLen)
                {
                    roffset = romLen == 0 ? 0 : roffset % romLen;
                }
                offset = (int)roffset;
                

                //offset = memory.Rom.Length == 0 ? 0 : (int)((address - 0x08000000) % (uint)memory.Rom.Length);
                //buffer = memory.Rom.AsSpan(offset, size);
                buffer = new Span<byte>(memory.Rom + offset, size);
                return MemoryRegion.Rom;
            case 0x0E:
                offset = (int)((address - 0x0E000000) % (uint)memory.Sram.Length);
                buffer = memory.Sram.AsSpan(offset, size);
                return MemoryRegion.Sram;
            default:
                buffer = [];
                Console.WriteLine($"Address Accessed: 0x{address:x8}");
                return MemoryRegion.Unused;
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