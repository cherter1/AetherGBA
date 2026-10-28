using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ThumbMode;

public sealed class Format1Shifts
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LSL_OffsetIsZero_NZFlagsSetLowAndVCFlagsUnchanged(bool initialFlagValue)
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: lsl r0, r1, #0
        bus.Write16(0x02000000, 0x0008);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x12345678;
        cpu.Registers.Cpsr.ThumbState = true;

        // set flags high
        cpu.SetCarry(initialFlagValue);
        cpu.SetOverflow(initialFlagValue);
        cpu.SetNegative(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, cpu.Registers[0]);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Zero);

        Assert.Equal(initialFlagValue, cpu.Registers.Cpsr.Overflow);
        Assert.Equal(initialFlagValue, cpu.Registers.Cpsr.Carry);
    }

    [Fact]
    public void LSL_NormalShift_CarryGetsOld31Bit()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: lsl r0, r1, #1
        bus.Write16(0x02000000, 0x0048);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x80000001;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x2u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Overflow);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void LSL_ResultBecomesNegative_NFlagGetsSetHigh()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: lsl r0, r1, #1
        bus.Write16(0x02000000, 0x0048);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x40000000;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x80000000, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Negative);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Overflow);
        Assert.False(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void LSL_ZeroResult_CarryClearedAndZeroFlagSetHigh()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: lsl r0, r1, #31
        bus.Write16(0x02000000, 0x07c8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x10;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Zero);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Overflow);
        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void LSR_NormalShiftOldBit0Set_CarryGetsSetHigh()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: lsr r0, r1, #1
        bus.Write16(0x02000000, 0x0848);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x80000001;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x40000000u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Overflow);
        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void LSR_NormalShiftOldBit0NotSet_CarryCleared()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: lsr r0, r1, #1
        bus.Write16(0x02000000, 0x0848);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x80000000;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x40000000u, cpu.Registers[0]);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Overflow);
        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void LSR_Shift31Old30BitSetHigh_CarryFlagSetHigh()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: lsr r0, r1, #31
        bus.Write16(0x02000000, 0x0fc8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x40000000;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0u, cpu.Registers[0]);

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Overflow);
        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void LSR_Bit30NotSet_CarryFlagClearedProducesZero()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: lsr r0, r1, #31
        bus.Write16(0x02000000, 0x0fc8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x80000000;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x1u, cpu.Registers[0]);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Overflow);
        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void LSR_Shift32With31BitSetHigh_CarryAndZeroFlagSetHigh()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: lsr r0, r1, #32
        bus.Write16(0x02000000, 0x0808);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x80000000;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);

        Assert.False(cpu.Registers.Cpsr.Overflow);
        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void LSR_Shift32With31BitSetLow_CarrySetLowProducesZero()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: lsr r0, r1, #32
        bus.Write16(0x02000000, 0x0808);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x7fffffff;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Zero);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Overflow);
        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void ASR_PositiveValueShift_CarrySetHigh()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: asr r0, r1, #1
        bus.Write16(0x02000000, 0x1048);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x40000001;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x20000000u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Overflow);
        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void ASR_NegativeValueShift_SignExtendsNegativeFlagSetHigh()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: asr r0, r1, #1
        bus.Write16(0x02000000, 0x1048);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x80000000;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xC0000000u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Negative);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void ASR_PositiveShift4_SignExtendsWithCarryFromBit3()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: asr r0, r1, #4
        bus.Write16(0x02000000, 0x1108);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0xF0000008;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xFF000000u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void ASR_PositiveValueShift31_ZeroAndCarrySetHigh()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: asr r0, r1, #31
        bus.Write16(0x02000000, 0x17C8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x40000000;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void ASR_Shift32WithNegativeSource_NegativeAndCarryFlagSetHigh()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: asr r0, r1, #32
        bus.Write16(0x02000000, 0x1008);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x80000000;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffffffffu, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void ASR_Shift32WithPositiveSource_ZeroFlagSetHigh()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: asr r0, r1, #32
        bus.Write16(0x02000000, 0x1008);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x7fffffff;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Zero);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }
}