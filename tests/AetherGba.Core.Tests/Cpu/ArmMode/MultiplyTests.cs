using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ArmMode;

public sealed class MultiplyTests
{
    [Fact]
    public void MUL_PositiveOperandsAndResultHasNoOverflow_FlagsUnchangedResultCalculated()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mul r0, r1, r2
        bus.Write32(0x02000000, 0xe0000291);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0;
        cpu.Registers[1] = 3;
        cpu.Registers[2] = 7;
        cpu.SetNegative(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(21u, cpu.Registers[0]);

        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void MULS_PositiveOperandsAndResult_NegativeAndZeroFlagCleared()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: muls r0, r1, r2
        bus.Write32(0x02000000, 0xe0100291);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0;
        cpu.Registers[1] = 3;
        cpu.Registers[2] = 7;
        cpu.SetNegative(true);
        cpu.SetZero(true);
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(21u, cpu.Registers[0]);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void MULS_ZeroResult_ZeroFlagSetNegativeCleared()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: muls r0, r1, r2
        bus.Write32(0x02000000, 0xe0100291);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 1;
        cpu.Registers[1] = 0;
        cpu.Registers[2] = 7;
        cpu.SetNegative(true);
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0u, cpu.Registers[0]);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void MULS_ResultWrapsToZero_NegativeFlaClearedZeroFlagSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: muls r0, r1, r2
        bus.Write32(0x02000000, 0xe0100291);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 1;
        cpu.Registers[1] = 0x80000000;
        cpu.Registers[2] = 2;
        cpu.SetNegative(true);
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0u, cpu.Registers[0]);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void MLA_BasicOperandsAndAccumulateWithNoOverflow_FlagsUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mla r0, r1, r2, r3
        bus.Write32(0x02000000, 0xe0203291);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 1;
        cpu.Registers[1] = 3;
        cpu.Registers[2] = 7;
        cpu.Registers[3] = 5;
        cpu.SetNegative(true);
        cpu.SetZero(true);
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(26u, cpu.Registers[0]);

        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void MLA_WrapsAround_FlagsUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mla r0, r1, r2, r3
        bus.Write32(0x02000000, 0xe0203291);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0;
        cpu.Registers[1] = 0xFFFFFFFF;
        cpu.Registers[2] = 2;
        cpu.Registers[3] = 3;
        cpu.SetNegative(true);
        cpu.SetZero(true);
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(1u, cpu.Registers[0]);

        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void MLA_ZeroResult_NegativeFlagClearedAndZeroFlagSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mlas r0, r1, r2, r3
        bus.Write32(0x02000000, 0xe0303291);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0;
        cpu.Registers[1] = 3;
        cpu.Registers[2] = 7;
        cpu.Registers[3] = 0xFFFFFFeb;
        cpu.SetNegative(true);
        cpu.SetZero(true);
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0u, cpu.Registers[0]);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void MLA_NegativeResult_NegativeFlagSetAndZeroFlagCleared()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mlas r0, r1, r2, r3
        bus.Write32(0x02000000, 0xe0303291);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0;
        cpu.Registers[1] = 3;
        cpu.Registers[2] = 7;
        cpu.Registers[3] = 0xFFFFFFea;
        cpu.SetNegative(true);
        cpu.SetZero(true);
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffffffff, cpu.Registers[0]);

        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }
}