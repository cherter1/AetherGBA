using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ThumbMode;

public sealed class Format18UnconditionalBranch
{
    [Fact]
    public void B_MaxBackwardJump_VisiblePcDecremented2048FlagsUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x03000100: b -2048
        bus.Write16(0x03000100, 0xe400);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x03000100;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);
        cpu.SetNegative(true);
        cpu.SetOverflow(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02fff904u, cpu.Registers.ProgramCounter);

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void B_MaxForwardJump_VisiblePcIncrements2046FlagsUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000100: b +2046
        bus.Write16(0x02000100, 0xe3ff);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);
        cpu.SetNegative(true);
        cpu.SetOverflow(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000902u, cpu.Registers.ProgramCounter);

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void B_ForwardJumpZero_VisiblePcSetAsNextInstruction()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000100: b +0
        bus.Write16(0x02000100, 0xe000);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000104u, cpu.Registers.ProgramCounter);
    }
}