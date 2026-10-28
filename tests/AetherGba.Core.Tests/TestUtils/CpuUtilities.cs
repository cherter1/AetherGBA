using AetherGba.Core.Cpu;
using AetherGba.Core.Interrupts;
using AetherGba.Core.Memory;

namespace AetherGba.Core.Tests.TestUtils;

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