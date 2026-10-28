using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ArmMode;

public sealed class BlockDataTransferTests
{
    [Fact]
    public void STM_Arm_IncrementAfter_MemoryIsSetCorrectlyAndBaseIsWrittenBack()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: stmia r4!, {r0-r2}
        bus.Write32(0x02000000, 0xE8A40007);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[4] = 0x02000100;
        cpu.Registers[0] = 0x11111111;
        cpu.Registers[1] = 0x22222222;
        cpu.Registers[2] = 0x33333333;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0200010Cu, cpu.Registers[4]);
        Assert.Equal(0x11111111u, bus.Read32(0x02000100u));
        Assert.Equal(0x22222222u, bus.Read32(0x02000104u));
        Assert.Equal(0x33333333u, bus.Read32(0x02000108u));
    }

    [Fact]
    public void LDM_Arm_IncrementAfter_MemoryIsLoadedCorrectlyAndBaseIsWrittenBack()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        bus.Write32(0x02000100, 0x11111111u);
        bus.Write32(0x02000104, 0x22222222u);
        bus.Write32(0x02000108, 0x33333333u);

        // 0x02000000: ldmia r4!, {r0-r2}
        bus.Write32(0x02000000, 0xE8B40007);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[4] = 0x02000100;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0200010Cu, cpu.Registers[4]);
        Assert.Equal(0x11111111u, cpu.Registers[0]);
        Assert.Equal(0x22222222u, cpu.Registers[1]);
        Assert.Equal(0x33333333u, cpu.Registers[2]);
    }
}