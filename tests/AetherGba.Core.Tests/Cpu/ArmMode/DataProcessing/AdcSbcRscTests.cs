using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ArmMode.DataProcessing;

public sealed class AdcSbcRscTests
{
    [Fact]
    public void ADCS__test()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: adcs r0, r1, r2
        bus.Write32(0x02000000, 0xE0b10002);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x1;
        cpu.Registers[1] = 0;
        cpu.Registers[2] = 0xFFFFFFFF;
        cpu.SetCarry(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void SBCS__test()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sbcs r0, r1, r2
        bus.Write32(0x02000000, 0xE0d10002);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x1;
        cpu.Registers[1] = 0x7fffffff;
        cpu.Registers[2] = 0;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x7ffffffeu, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void RSCS__test()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: rscs r0, r1, r2
        bus.Write32(0x02000000, 0xE0f10002);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x1;
        cpu.Registers[1] = 0;
        cpu.Registers[2] = 0x7fffffff;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x7ffffffeu, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }
}