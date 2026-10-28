using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ThumbMode;

public sealed class Format4AluOps
{
    [Fact]
    public void AND_OperandsDontShareAnyHighBits_ZeroSetCVUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: and r0, r1
        bus.Write16(0x02000000, 0x4008);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xf0;
        cpu.Registers[1] = 0x0f;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);
        cpu.SetOverflow(true);
        cpu.SetNegative(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Zero);

        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void EOR_RightOperandHasBottom4BitsUnset_ResultHasFAndCVUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: eor r0, r1
        bus.Write16(0x02000000, 0x4048);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xff;
        cpu.Registers[1] = 0xf0;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);
        cpu.SetOverflow(true);
        cpu.SetNegative(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xfu, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Overflow);

        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void LSL_30BitSetShiftsOne_VUnchangedNegativeSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: lsl r0, r1
        bus.Write16(0x02000000, 0x4088);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x40000000;
        cpu.Registers[1] = 1;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);
        cpu.SetOverflow(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x80000000u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Negative);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void LSL_ZeroShift_RegisterUnchangedCVUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: lsl r0, r1
        bus.Write16(0x02000000, 0x4088);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x12345678;
        cpu.Registers[1] = 0;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);
        cpu.SetOverflow(true);
        cpu.SetZero(true);
        cpu.SetNegative(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void LSR_ZeroBitSetShiftOne_VUnchangedCarrySet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: lsr r0, r1
        bus.Write16(0x02000000, 0x40c8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x80000001;
        cpu.Registers[1] = 1;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetZero(true);
        cpu.SetNegative(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x40000000u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void LSR_31BitSetAndShift32_ZeroResultCarrySet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: lsr r0, r1
        bus.Write16(0x02000000, 0x40c8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x80000000;
        cpu.Registers[1] = 32;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetNegative(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);

        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void ASR_NegativeShiftOne_CarryClearedVUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: asr r0, r1
        bus.Write16(0x02000000, 0x4108);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x80000000;
        cpu.Registers[1] = 1;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetCarry(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xc0000000u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Negative);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void ASR_NegativeShift32Plus_SignExtendedCarryFlagSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: asr r0, r1
        bus.Write16(0x02000000, 0x4108);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x80000000;
        cpu.Registers[1] = 32;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffffffffu, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void ASR_PositiveShift32Plus_SignExtendedCarryCleared()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: asr r0, r1
        bus.Write16(0x02000000, 0x4108);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x40000000;
        cpu.Registers[1] = 32;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetZero(true);
        cpu.SetCarry(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Zero);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void ADC_AddZeroWithCarry_ZeroResultSetsCarry()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: adc r0, r1
        bus.Write16(0x02000000, 0x4148);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffffffff;
        cpu.Registers[1] = 0;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetCarry(true);

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
    public void SBC_LeftGreaterThanRightCarrySetHigh_NoBorrow()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sbc r0, r1
        bus.Write16(0x02000000, 0x4188);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 5;
        cpu.Registers[1] = 3;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetCarry(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(2u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void SBC_LeftGreaterThanRightCarrySetLow_ResultUsesBorrowAndCarrySetBackHigh()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sbc r0, r1
        bus.Write16(0x02000000, 0x4188);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 5;
        cpu.Registers[1] = 3;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(1u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void ROR_NegativeNumberLastBitOutSetHigh_NegativeNumberCarrySetHigh()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ror r0, r1
        bus.Write16(0x02000000, 0x41c8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x80000001;
        cpu.Registers[1] = 1;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xc0000000u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Negative);

        Assert.False(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void TST_OperandsHaveNoSharedBits_ZeroSetHighCVUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: tst r0, r1
        bus.Write16(0x02000000, 0x4208);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x0f;
        cpu.Registers[1] = 0xf0;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetCarry(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0fu, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Zero);

        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void NEG_SourceRegOne_CarrySetLowNegativeSetHigh()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: neg r0, r1
        bus.Write16(0x02000000, 0x4248);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x0f;
        cpu.Registers[1] = 1;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetZero(true);
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
    public void NEG_SourceRegZero_NoBorrowZeroSetHigh()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: neg r0, r1
        bus.Write16(0x02000000, 0x4248);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x0f;
        cpu.Registers[1] = 0;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);

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
    public void CMP_RightGreaterThanLeftOperand_NegativeFlagSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: cmp r0, r1
        bus.Write16(0x02000000, 0x4288);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 3;
        cpu.Registers[1] = 5;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetCarry(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x3u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Negative);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void CMN_MaxPlusOne_ZeroAndCarrySet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: cmn r0, r1
        bus.Write16(0x02000000, 0x42c8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffffffff;
        cpu.Registers[1] = 1;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetNegative(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffffffffu, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void ORR_BitsOtherThanTopBitSet_NZSetLowCVUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: orr r0, r1
        bus.Write16(0x02000000, 0x4308);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x0f;
        cpu.Registers[1] = 0xf0;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetCarry(true);
        cpu.SetZero(true);
        cpu.SetNegative(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffu, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Overflow);

        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void MUL_NoBorrowMultiply_CDestroyedVUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mul r0, r1
        bus.Write16(0x02000000, 0x4348);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 3;
        cpu.Registers[1] = 7;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetCarry(true);
        cpu.SetZero(true);
        cpu.SetNegative(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x15u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Overflow);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void MUL_MultiplyZero_ZeroFlagSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mul r0, r1
        bus.Write16(0x02000000, 0x4348);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 3;
        cpu.Registers[1] = 0;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetCarry(true);
        cpu.SetNegative(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x00u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Zero);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void BIC_BottomFourBitsSwapped_CVUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: bic r0, r1
        bus.Write16(0x02000000, 0x4388);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xff;
        cpu.Registers[1] = 0xf0;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetCarry(true);
        cpu.SetNegative(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0fu, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Carry);

        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
    }

    [Fact]
    public void MVN_MoveZero_MaxValueResultCVUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mvn r0, r1
        bus.Write16(0x02000000, 0x43c8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xff;
        cpu.Registers[1] = 0x0;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetCarry(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffffffffu, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Negative);

        Assert.False(cpu.Registers.Cpsr.Zero);
    }
}