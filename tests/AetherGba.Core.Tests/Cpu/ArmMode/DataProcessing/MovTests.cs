using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ArmMode.DataProcessing;

public sealed class MovTests
{
    [Fact]
    public void LSLS_RegisterOperandsShiftedByOne_MovesShiftedValueIntoRegister()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: movs r0, r1, lsl r0
        // aka lsls r0, r1, r0
        bus.Write32(0x02000000, 0xe1b00011);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 1;
        cpu.Registers[1] = 0xFFF;
        cpu.SetCarry(true);
        cpu.SetNegative(true);
        cpu.SetOverflow(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x1FFEu, cpu.Registers[0]);
        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.False(cpu.Registers.Cpsr.Zero);
    }

    [Theory]
    [InlineData(0x3, true)]
    [InlineData(0x2, false)]
    public void LSLS_RegisterOperandsShiftedBy32_ResultZeroIntoRegisterAndCarrySetOnBitZero(uint preshiftValue, bool expectedCarry)
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: movs r0, r1, lsl r0
        // aka lsls r0, r1, r0
        bus.Write32(0x02000000, 0xe1b00011);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 32;
        cpu.Registers[1] = preshiftValue;
        cpu.SetCarry(!expectedCarry);
        cpu.SetNegative(true);
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0u, cpu.Registers[0]);
        Assert.Equal(expectedCarry, cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void LSLS_RegisterOperandsLeftShiftedByGreaterThan32_ResultZeroIntoRegisterAndCarryNotSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: movs r0, r1, lsl r0
        // aka lsls r0, r1, r0
        bus.Write32(0x02000000, 0xe1b00011);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 33;
        cpu.Registers[1] = 0x3;
        cpu.SetCarry(true);
        cpu.SetNegative(true);
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0u, cpu.Registers[0]);
        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void LSLS_RegisterOpeandPcLeftShiftedByRegisterWithZero_CarryUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: movs r0, pc, lsl r0
        // aka lsls r0, pc, r0
        bus.Write32(0x02000000, 0xe1b0001f);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0;
        cpu.SetCarry(true);
        cpu.SetZero(true);
        cpu.SetNegative(true);
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0200000cu, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Theory]
    [InlineData(0x80000fff, true)]
    [InlineData(0x70000fff, false)]
    public void LSRS_ImmediateOperandRightShift32_ResultZeroIntoRegisterAndCarryBecomes31Bit(uint preshiftValue, bool expectedCarry)
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: movs r0, r1, lsr #32
        // aka lsrs r0, r1, #32
        bus.Write32(0x02000000, 0xe1b00021);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = preshiftValue;
        cpu.SetNegative(true);
        cpu.SetOverflow(true);
        cpu.SetCarry(!expectedCarry);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0u, cpu.Registers[0]);
        Assert.Equal(expectedCarry, cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void ASRS_ImmediateOperandArithmaticRightShift32_ResultAndCarryFilledWith31Bit()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: movs r0, r1, asr #32
        // aka asrs r0, r1, #32
        bus.Write32(0x02000000, 0xe1b00041);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x80000fff;
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xFFFFFFFF, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ASRS_RegisterOperandArithmaticShiftRight0_CarryUnaffectedResultStaysSame(bool initialCarry)
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: movs r0, r1, asr r0
        // aka asrs r0, r1, r0
        bus.Write32(0x02000000, 0xe1b00051);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x80000fff;
        cpu.Registers[0] = 0;
        cpu.SetOverflow(true);
        cpu.SetCarry(initialCarry);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x80000fff, cpu.Registers[0]);
        Assert.Equal(initialCarry, cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Theory]
    [InlineData(32)]
    [InlineData(33)]
    public void ASRS_RegisterOperandArithmaticShiftRight32OrGreater_ResultAndCarryFilledWith31Bit(uint shiftAmount)
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: movs r0, r1, asr r0
        // aka asrs r0, r1, r0
        bus.Write32(0x02000000, 0xe1b00051);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0xc0000000;
        cpu.Registers[0] = shiftAmount;
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffffffff, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void RRX_CarrySetHigh_MsbSetHighCarrySetToOldLsb()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: movs r0, r1, rrx
        // aka rrxs r0, r1
        bus.Write32(0x02000000, 0xe1b00061);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x12345678;
        cpu.SetOverflow(true);
        cpu.SetCarry(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x891a2b3c, cpu.Registers[0]);
        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void RRX_CarrySetLow_MsbSetLowCarrySetToOldLsb()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: movs r0, r1, rrx
        // aka rrxs r0, r1
        bus.Write32(0x02000000, 0xe1b00061);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[1] = 0x12345671;
        //0 9 1 a 2 b 3 8
        cpu.SetOverflow(true);
        cpu.SetNegative(true);
        cpu.SetCarry(false);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x091a2b38u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }
}