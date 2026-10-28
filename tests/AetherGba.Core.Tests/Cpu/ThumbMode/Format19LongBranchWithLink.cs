using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ThumbMode;

public sealed class Format19LongBranchWithLink
{
    [Fact]
    public void BL_ZeroOffset_LrSetToVisiblePcPlusOnePcIncrementsFour()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000100: bl +0
        bus.Write32(0x02000100, 0xf800f000);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);
        cpu.SetNegative(true);
        cpu.SetOverflow(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.Equal(0x02000104u, cpu.Registers.ProgramCounter);
        Assert.Equal(0x02000105u, cpu.Registers.LinkRegister);

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void BL_NegativeFourOffset_PcSameAsOriginalInstructionLrPlusFive()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000100: bl -4
        bus.Write32(0x02000100, 0xfffef7ff);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.Equal(0x02000100u, cpu.Registers.ProgramCounter);
        Assert.Equal(0x02000105u, cpu.Registers.LinkRegister);
    }

    [Fact]
    public void BL_PositiveEightOffset_PcIncrementsOffsetPlusFour()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000100: bl +8
        bus.Write32(0x02000100, 0xf804f000);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.Equal(0x0200010cu, cpu.Registers.ProgramCounter);
        Assert.Equal(0x02000105u, cpu.Registers.LinkRegister);
    }
}