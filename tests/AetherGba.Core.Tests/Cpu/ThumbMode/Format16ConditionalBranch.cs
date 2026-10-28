using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ThumbMode;

public sealed class Format16ConditionalBranch
{
    [Theory]
    [InlineData(Condition.Eq, false, true, true, true)]
    [InlineData(Condition.Ne, true, false, false, false)]
    [InlineData(Condition.Cs, true, true, true, false)]
    [InlineData(Condition.Cc, false, false, false, true)]
    [InlineData(Condition.Mi, true, false, false, false)]
    [InlineData(Condition.Pl, true, true, false, false)]
    [InlineData(Condition.Vs, true, false, false, false)]
    [InlineData(Condition.Vc, true, false, true, false)]
    [InlineData(Condition.Hi, true, false, false, false)]
    [InlineData(Condition.Ls, false, false, false, true)]
    [InlineData(Condition.Ge, true, true, false, false)]
    [InlineData(Condition.Lt, true, true, true, false)]
    [InlineData(Condition.Gt, true, true, false, false)]
    [InlineData(Condition.Le, false, true, true, false)]
    public void BCOND_ConditionsFailed_PcIncrementsByTwoBranchNotTakenFlagsUnchanged(Condition cond, bool zero, bool negative, bool overflow, bool carry)
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();
        var instruction = 0xd000 | ((ushort)cond << 8) | 3;

        // 0x02000000: b{cond} +6
        bus.Write16(0x02000000, (ushort)instruction);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetZero(zero);
        cpu.SetNegative(negative);
        cpu.SetOverflow(overflow);
        cpu.SetCarry(carry);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000002u, cpu.Registers.ProgramCounter);

        Assert.Equal(zero, cpu.Registers.Cpsr.Zero);
        Assert.Equal(negative, cpu.Registers.Cpsr.Negative);
        Assert.Equal(overflow, cpu.Registers.Cpsr.Overflow);
        Assert.Equal(carry, cpu.Registers.Cpsr.Carry);
    }

    [Theory]
    [InlineData(Condition.Eq, true, true, true, true)]
    [InlineData(Condition.Ne, false, false, false, false)]
    [InlineData(Condition.Cs, true, true, true, true)]
    [InlineData(Condition.Cc, false, false, false, false)]
    [InlineData(Condition.Mi, true, true, false, false)]
    [InlineData(Condition.Pl, true, false, false, false)]
    [InlineData(Condition.Vs, true, false, true, false)]
    [InlineData(Condition.Vc, true, false, false, false)]
    [InlineData(Condition.Hi, false, false, false, true)]
    [InlineData(Condition.Ls, true, false, false, false)]
    [InlineData(Condition.Ge, true, true, true, false)]
    [InlineData(Condition.Lt, true, true, false, false)]
    [InlineData(Condition.Gt, false, true, true, false)]
    [InlineData(Condition.Le, true, false, true, false)]
    [InlineData(Condition.Al, true, false, true, false)]
    public void BCOND_ConditionsPassed_VisiblePcIncrementsBySixBranchTakenFlagsUnchanged(Condition cond, bool zero, bool negative, bool overflow, bool carry)
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();
        var instruction = 0xd000 | ((ushort)cond << 8) | 3;

        // 0x02000000: b{cond} +6
        bus.Write16(0x02000000, (ushort)instruction);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetZero(zero);
        cpu.SetNegative(negative);
        cpu.SetOverflow(overflow);
        cpu.SetCarry(carry);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x0200000au, cpu.Registers.ProgramCounter);

        Assert.Equal(zero, cpu.Registers.Cpsr.Zero);
        Assert.Equal(negative, cpu.Registers.Cpsr.Negative);
        Assert.Equal(overflow, cpu.Registers.Cpsr.Overflow);
        Assert.Equal(carry, cpu.Registers.Cpsr.Carry);
    }

    [Fact]
    public void BCOND_ConditionPassedWithPositive254_VisiblePcJumpsBy254()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: bne +254
        bus.Write16(0x02000000, 0xd17f);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000102u, cpu.Registers.ProgramCounter);
    }

    [Fact]
    public void BCOND_ConditionPassedWithNegative256_VisiblePcJumpsBackwardBy256()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000100: bne -256
        bus.Write16(0x02000100, 0xd180);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x02000004u, cpu.Registers.ProgramCounter);
    }
}