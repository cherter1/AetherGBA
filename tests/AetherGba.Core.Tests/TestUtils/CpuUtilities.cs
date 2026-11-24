using AetherGba.Core.Common;
using AetherGba.Core.Cpu;
using AetherGba.Core.Dma;
using AetherGba.Core.Interrupts;
using AetherGba.Core.Memory;
using AetherGba.Core.Video;

namespace AetherGba.Core.Tests.TestUtils;

public class CpuUtilities
{
    public static (Arm7Tdmi Cpu, GbaBus Bus) CreateCpu()
    {
        var memory = new GbaMemory();
        var interrupts = new InterruptController(memory);
        var dma = new DmaController(interrupts, memory);
        var ppu = new Ppu(interrupts, dma, memory, new GbaEventHandler());
        var bus = new GbaBus(memory, ppu);
        memory.Io.ConnectInterruptController(interrupts);
        return (new Arm7Tdmi(bus, interrupts), bus);
    }
}