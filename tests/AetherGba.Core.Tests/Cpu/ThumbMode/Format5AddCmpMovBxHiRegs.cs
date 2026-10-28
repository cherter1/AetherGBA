using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ThumbMode;

public sealed class Format5AddCmpMovBxHiRegs
{
    [Fact]
    public void BX_HiRegLsbSet_StaysThumbModeAndSetsPcCorrectly()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000100: bx lr
        bus.Write16(0x02000100, 0x4770);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000100;
        cpu.Registers[14] = 0x02000201;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetNegative(true);
        cpu.SetCarry(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000200u, cpu.Registers.ProgramCounter);
        Assert.True(cpu.Registers.Cpsr.ThumbState);

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void BX_HiRegLsbNotSet_SwapsToArmModePcSetCorrectly()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000100: bx lr
        bus.Write16(0x02000100, 0x4770);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000100;
        cpu.Registers[14] = 0x02000200;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetNegative(true);
        cpu.SetCarry(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000200u, cpu.Registers.ProgramCounter);
        Assert.False(cpu.Registers.Cpsr.ThumbState);

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void BX_LoReg0BitSet_StaysThumbModePcSetCorrectly()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000100: bx r0
        bus.Write16(0x02000100, 0x4700);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000100;
        cpu.Registers[0] = 0x02000201;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetNegative(true);
        cpu.SetCarry(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000200u, cpu.Registers.ProgramCounter);
        Assert.True(cpu.Registers.Cpsr.ThumbState);

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void BX_LoReg0BitNotSet_SwapsToArmModePcSetCorrectly()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000100: bx r0
        bus.Write16(0x02000100, 0x4700);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000100;
        cpu.Registers[0] = 0x02000200;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetNegative(true);
        cpu.SetCarry(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000200u, cpu.Registers.ProgramCounter);
        Assert.False(cpu.Registers.Cpsr.ThumbState);

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void BX_PcUnaligned_SwapsToArmModePcSetToWordAlignedVisiblePc()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000102: bx pc
        bus.Write16(0x02000102, 0x4778);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000102;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetNegative(true);
        cpu.SetCarry(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000104u, cpu.Registers.ProgramCounter);
        Assert.False(cpu.Registers.Cpsr.ThumbState);

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void BX_PcAligned_SwapsToArmMode()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000100: bx pc
        bus.Write16(0x02000100, 0x4778);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetNegative(true);
        cpu.SetCarry(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000104u, cpu.Registers.ProgramCounter);
        Assert.False(cpu.Registers.Cpsr.ThumbState);

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    /*[Fact]
    public void MOV_HiRegPcUnaligned_PcWordAlignedBeforeMov()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000102: mov r8, pc
        bus.Write16(0x02000102, 0x46f8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000102;
        cpu.Registers[8] = 0x0;
        cpu.SetThumbState(true);
        cpu.SetOverflow(true);
        cpu.SetNegative(true);
        cpu.SetCarry(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000104u, cpu.Registers[8]);

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }*/

    [Fact]
    public void MOV_LoRegPcAligned_VisiblePcMovedFlagsUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000100: mov r0, pc
        bus.Write16(0x02000100, 0x4678);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000100;
        cpu.Registers[0] = 0x0;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetNegative(true);
        cpu.SetCarry(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000104u, cpu.Registers[0]);

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void CMP_PcEqualToLoRd_CarrySetHighNoBorrowZeroSet()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000100: cmp r8, pc
        bus.Write16(0x02000100, 0x45f8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000100;
        cpu.Registers[8] = 0x02000104;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetOverflow(true);
        cpu.SetNegative(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000104u, cpu.Registers[8]);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Zero);

        Assert.False(cpu.Registers.Cpsr.Negative);
        Assert.False(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void CMP_PcLessThanLoRd_CarrySetLowForBorrowAndNegativeSetHigh()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000100: cmp r0, pc
        bus.Write16(0x02000100, 0x4578);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000100;
        cpu.Registers[0] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);
        cpu.SetOverflow(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000100u, cpu.Registers[0]);
        Assert.True(cpu.Registers.Cpsr.Negative);

        Assert.False(cpu.Registers.Cpsr.Carry);
        Assert.False(cpu.Registers.Cpsr.Overflow);
        Assert.False(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void ADD_HiRegPcAligned_VisiblePcAddedToOperand()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000100: add r8, pc
        bus.Write16(0x02000100, 0x44f8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000100;
        cpu.Registers[8] = 0x20;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);
        cpu.SetOverflow(true);
        cpu.SetNegative(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000124u, cpu.Registers[8]);

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Negative);
    }

    /*
    [Fact]
    public void ADD_LoRegPcUnaligned_WordAlignsVisiblePc()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000102: add r0, pc
        bus.Write16(0x02000102, 0x4478);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000102;
        cpu.Registers[0] = 0x20;
        cpu.SetThumbState(true);
        cpu.SetCarry(true);
        cpu.SetOverflow(true);
        cpu.SetNegative(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000124u, cpu.Registers[0]);

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Negative);
    }
*/
}