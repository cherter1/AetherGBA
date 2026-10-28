using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ThumbMode;

public sealed class Format2AddSub
{
    [Fact]
    public void ADD_SimpleAdd_NoFlagsSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: add r0, r1, #1
        bus.Write16(0x02000000, 0x1c48);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x0000000e;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0000000fu, cpu.Registers[0]);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Overflow);
        Assert.False(cpu.Registers.Cpsr.Carry);
    }

    [Fact]
    public void ADD_AddPositiveNumberToZero_ValueUnchangedAndFlagsSetLow()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: add r0, r1, #0
        bus.Write16(0x02000000, 0x1c08);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x12345678;
        cpu.Registers.Cpsr.ThumbState = true;

        cpu.SetCarry(true);
        cpu.SetZero(true);
        cpu.SetNegative(true);
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, cpu.Registers[0]);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Overflow);
        Assert.False(cpu.Registers.Cpsr.Carry);
    }

    [Fact]
    public void ADD_AddZeroToZero_ZeroFlagSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: add r0, r1, #0
        bus.Write16(0x02000000, 0x1c08);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x0;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void ADD_ResultHas31BitSet_OverflowFlagSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: add r0, r1, #1
        bus.Write16(0x02000000, 0x1c48);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x7fffffff;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x80000000u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void ADD_ValueWrapsToZero_ZeroAndCarryFlagSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: add r0, r1, #1
        bus.Write16(0x02000000, 0x1c48);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0xffffffff;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void ADD_UnsignedCarryWithNonZeroResult_CarryFlagSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: add r0, r1, #7
        bus.Write16(0x02000000, 0x1dc8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0xfffffffe;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x5u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void ADD_MaxImmNoCarry_NoFlagsSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: add r0, r1, #7
        bus.Write16(0x02000000, 0x1dc8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x8;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xfu, cpu.Registers[0]);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void ADD_MaxImmToNegativeNumber_NoOverFlowAndNegativeFlagSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: add r0, r1, #7
        bus.Write16(0x02000000, 0x1dc8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x80000000;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x80000007u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Negative);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void SUB_SimpleSubtractNoBorrow_CarryFlagSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sub r0, r1, #1
        bus.Write16(0x02000000, 0x1e48);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x10;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xfu, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void SUB_SubtractToZero_CarryAndZeroFlagSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sub r0, r1, #1
        bus.Write16(0x02000000, 0x1e48);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x1;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void SUB_PositiveMinusZero_CarryFlagSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sub r0, r1, #0
        bus.Write16(0x02000000, 0x1e08);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x12345678;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void SUB_ZeroMinusZero_CarryAndZeroFlagSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sub r0, r1, #0
        bus.Write16(0x02000000, 0x1e08);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x0;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void SUB_ZeroMinusOne_CarryNotSetAndNegativeSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sub r0, r1, #1
        bus.Write16(0x02000000, 0x1e48);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x0;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffffffffu, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Negative);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void SUB_BorrowWithMaxImm_CarryNotSetAndNegativeSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sub r0, r1, #7
        bus.Write16(0x02000000, 0x1fc8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x3;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xfffffffcu, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Negative);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void SUB_NegativeWrapsToPositiveResult_OverFlowAndCarrySet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sub r0, r1, #1
        bus.Write16(0x02000000, 0x1e48);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x80000000;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x7fffffffu, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Overflow);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void SUB_NegativeResultWithoutOverflow_CarryAndNegativeSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sub r0, r1, #7
        bus.Write16(0x02000000, 0x1fc8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x80000007;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x80000000u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Negative);

        Assert.False(cpu.Registers.Cpsr.Overflow);
        Assert.False(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void SUB_MaxImmToZeroResult_CarryAndZeroSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sub r0, r1, #7
        bus.Write16(0x02000000, 0x1fc8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x7;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void SUB_RdSameAsRs_FlagsAndResultSetCorrectly()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sub r4, r4, r2
        bus.Write16(0x02000000, 0x1aa4);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[4] = 0x8c;
        cpu.Registers[2] = 0x50;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetNegative(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x3cu, cpu.Registers[4]);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }
}