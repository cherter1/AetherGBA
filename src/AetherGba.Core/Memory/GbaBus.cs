using System.Buffers.Binary;
using System.Numerics;
using AetherGba.Core.Bios;
using GbaCartridge = AetherGba.Core.Cartridge.Cartridge;

namespace AetherGba.Core.Memory;

public sealed class GbaBus(GbaMemory memory)
{
    public void LoadBios(BiosImage? bios)
    {
        if (bios is not null)
        {
            Array.Copy(bios.Bytes, memory.Bios, Math.Min(memory.Bios.Length, bios.Bytes.Length));
        }
    }

    public void LoadCartridge(GbaCartridge? cartridge) => memory.Rom = cartridge?.RomData ?? [];

    public uint Read32(uint address)
    {
        var aligned = address & 0xfffffffc;
        var region = ResolveRegion(aligned, 4, out var buffer);

        uint raw = region switch
        {
            MemoryRegion.Unused => 0,
            MemoryRegion.Io => memory.Io.ReadIo32Aligned(aligned),
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
            _ => (ushort)((buffer[1] << 8) | buffer[0])
        };
    }

    public byte Read8(uint address)
    {
        var region = ResolveRegion(address, 1, out var buffer);
        return region switch
        {
            MemoryRegion.Unused => 0,
            MemoryRegion.Io => memory.Io.ReadIo8(address),
            MemoryRegion.Sram => memory.Flash.Read8(address),
            _ => buffer[0]
        };
    }

    public void Write32(uint address, uint value)
    {
        address &= 0xfffffffc;
        var region = ResolveRegion(address, 4, out var buffer);

        switch (region)
        {
            case MemoryRegion.Bios or MemoryRegion.Rom or MemoryRegion.Unused:
                return;
            case MemoryRegion.Io:
                memory.Io.WriteIo32Aligned(address, value);
                break;
            default:
                BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
                break;
        }
    }

    public void Write16(uint address, ushort value)
    {
        address &= ~1u;
        var region = ResolveRegion(address, 2, out var buffer);
        switch (region)
        {
            case MemoryRegion.Bios or MemoryRegion.Rom or MemoryRegion.Unused:
                return;
            case MemoryRegion.Io:
                memory.Io.WriteIo16Aligned(address, value);
                break;
            default:
                buffer[1] = (byte)(value >> 8);
                buffer[0] = (byte)value;
                break;
        }
    }

    public void Write8(uint address, byte value)
    {
        var region = ResolveRegion(address, 1, out var buffer);
        switch (region)
        {
            case MemoryRegion.Bios or MemoryRegion.Rom or MemoryRegion.Unused:
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
    }

    private MemoryRegion ResolveRegion(uint address, int size, out Span<byte> buffer)
    {
        int offset;
        switch (address >> 24)
        {
            case 0x00:
                offset = (int)(address % (uint)memory.Bios.Length);
                buffer = memory.Bios.AsSpan()[offset..(offset + size)];
                return MemoryRegion.Bios;
            case 0x02:
                offset = (int)((address - 0x02000000) % (uint)memory.Ewram.Length);
                buffer = memory.Ewram.AsSpan()[offset..(offset + size)];
                return MemoryRegion.Ewram;
            case 0x03:
                offset = (int)((address - 0x03000000) % (uint)memory.Iwram.Length);
                buffer = memory.Iwram.AsSpan()[offset..(offset + size)];
                return MemoryRegion.Iwram;
            case 0x04:
                buffer = [];
                return MemoryRegion.Io;
            case 0x05:
                offset = (int)((address - 0x05000000) % (uint)memory.PaletteRam.Length);
                buffer = memory.PaletteRam.AsSpan()[offset..(offset + size)];
                return MemoryRegion.PaletteRam;
            case 0x06:
                offset = (int)((address - 0x06000000) % (uint)memory.Vram.Length);
                buffer = memory.Vram.AsSpan()[offset..(offset + size)];
                return MemoryRegion.Vram;
            case 0x07:
                offset = (int)((address - 0x07000000) % (uint)memory.Oam.Length);
                buffer = memory.Oam.AsSpan()[offset..(offset + size)];
                return MemoryRegion.Oam;
            case 0x08:
            case 0x09:
            case 0x0A:
            case 0x0B:
            case 0x0C:
            case 0x0D:
                offset = memory.Rom.Length == 0 ? 0 : (int)((address - 0x08000000) % (uint)memory.Rom.Length);
                buffer = memory.Rom.AsSpan()[offset..(offset + size)];
                return MemoryRegion.Rom;
            case 0x0E:
                offset = (int)((address - 0x0E000000) % (uint)memory.Sram.Length);
                buffer = memory.Sram.AsSpan()[offset..(offset + size)];
                return MemoryRegion.Sram;
            default:
                buffer = [];
                Console.WriteLine($"Address Accessed: 0x{address:x8}");
                return MemoryRegion.Unused;
        }
    }

    public int GetCpuAccessCycles(uint address, AccessWidth width, bool sequential)
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
            0x08 or 0x09 => GetGamePakRomCycles(waitState: 0, width, sequential),
            0x0A or 0x0B => GetGamePakRomCycles(waitState: 1, width, sequential),
            0x0C or 0x0D => GetGamePakRomCycles(waitState: 2, width, sequential),
            0x0E or 0x0F => GetSramCycles(width), //SRAM
            _ => 1
        };
    }

    public int GetGamePakRomCycles(int waitState, AccessWidth width, bool sequential)
    {
        int first;
        int second;
        switch (waitState)
        {
            case 0:
                first = DecodeFirstAccess((memory.Io.REG_WAITCNT >> 2) & 0b11);
                second = ((memory.Io.REG_WAITCNT >> 4) & 1) == 0 ? 2 : 1;
                break;
            case 1:
                first = DecodeFirstAccess((memory.Io.REG_WAITCNT >> 5) & 0b11);
                second = ((memory.Io.REG_WAITCNT >> 7) & 1) == 0 ? 4 : 1;
                break;
            case 2:
                first = DecodeFirstAccess((memory.Io.REG_WAITCNT >> 8) & 0b11);
                second = ((memory.Io.REG_WAITCNT >> 10) & 1) == 0 ? 8 : 1;
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

    public int GetSramCycles(AccessWidth width)
    {
        var cycles = DecodeFirstAccess(memory.Io.REG_WAITCNT & 0b11);
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
            _ => throw new ArgumentOutOfRangeException(nameof(value)),
        };
    }
}