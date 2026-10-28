using GbaEmulator.Core.Cpu;
using GbaEmulator.Core.Interrupts;
using GbaEmulator.Core.Memory;

namespace GbaEmulator.Core.Tests.TestUtils;

public class CpuUtilities
{
    public static (Arm7Tdmi Cpu, GbaBus Bus) CreateCpu()
    {
        var memory = new GbaMemory();
        var interrupts = new InterruptController(memory);
        var bus = new GbaBus(memory);
        memory.Io.ConnectInterruptController(interrupts);
        return (new Arm7Tdmi(bus, interrupts), bus);
    }
}