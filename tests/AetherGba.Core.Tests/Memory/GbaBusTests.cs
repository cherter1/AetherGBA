using System.Runtime.InteropServices;
using AetherGba.Core.Common;
using AetherGba.Core.Dma;
using AetherGba.Core.Input;
using AetherGba.Core.Interrupts;
using AetherGba.Core.Memory;
using AetherGba.Core.Video.Ppu;

namespace AetherGba.Core.Tests.Memory;

public sealed unsafe class VramReadWriteTests
{
    [Fact]
    public void Write32_Vram_writesLittleEndianCorrectly()
    {
        var memory = new GbaMemory();
        var interrupts = new InterruptController(memory);
        var dma = new DmaController(interrupts, memory);
        var ppu = new Ppu(interrupts, dma, memory, new GbaEventHandler());
        var bus = new GbaBus(memory, ppu);
        GbaBus.InitMemoryRegions();

        memory.Vram[0] = 0xed;
        memory.Vram[1] = 0xfe;
        memory.Vram[2] = 0xbe;
        memory.Vram[3] = 0xba;

        bus.Write32(0x06000000, 0x12345678);

        Assert.Equal(0x78, memory.Vram[0]);
        Assert.Equal(0x56, memory.Vram[1]);
        Assert.Equal(0x34, memory.Vram[2]);
        Assert.Equal(0x12, memory.Vram[3]);

        var read = bus.Read32(0x06_00_00_00);
        Assert.Equal(0x12345678u, read);
        var read16 = bus.Read16(0x06_00_00_00);
        Assert.Equal(0x5678u, read16);
    }

    [Fact]
    public void Write16_Vram_writesLittleEndianCorrectly()
    {
        var memory = new GbaMemory();
        var interrupts = new InterruptController(memory);
        var dma = new DmaController(interrupts, memory);
        var ppu = new Ppu(interrupts, dma, memory, new GbaEventHandler());
        var bus = new GbaBus(memory, ppu);
        GbaBus.InitMemoryRegions();

        memory.Vram[0] = 0xbe;
        memory.Vram[1] = 0xba;

        bus.Write16(0x06000000, 0x1234);
        Assert.Equal(0x34, memory.Vram[0]);
        Assert.Equal(0x12, memory.Vram[1]);

        var read = bus.Read16(0x06_00_00_00);
        Assert.Equal(0x1234, read);
    }

    [Fact]
    public void ReadThumbInstruction_ReadsCorrectValue()
    {
        var memory = new GbaMemory();
        var interrupts = new InterruptController(memory);
        var dma = new DmaController(interrupts, memory);
        var ppu = new Ppu(interrupts, dma, memory, new GbaEventHandler());
        var bus = new GbaBus(memory, ppu);
        GbaBus.InitMemoryRegions();
        memory.Rom = (byte*)NativeMemory.AllocZeroed(0x8);
        memory.RomLen = 0x8;
        memory.Rom[0] = 0xbe;
        memory.Rom[1] = 0xba;
        memory.Rom[2] = 0xbe;
        memory.Rom[3] = 0xba;

        var ins = bus.ReadThumbInstruction(0x08000002);
        var ins2 = bus.Read32(0x0A000000);
        Assert.Equal(0xbabe, ins);
        Assert.Equal(0xbabebabe, ins2);
    }
}