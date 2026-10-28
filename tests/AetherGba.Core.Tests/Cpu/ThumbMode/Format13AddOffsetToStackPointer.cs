using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ThumbMode;

public sealed class Format13AddOffsetToStackPointer
{
    [Fact]
    public void ADD_ZeroOffset_NoOpAndFlagsUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: add sp, #0
        bus.Write16(0x02000000, 0xb000);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03007000;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);
        cpu.SetNegative(true);
        cpu.SetZero(true);
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x03007000u, cpu.Registers.StackPointer);

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void ADD_FourOffset_StackPointerUpdatedPlusFour()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: add sp, #4
        bus.Write16(0x02000000, 0xb001);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03007000;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x03007004u, cpu.Registers.StackPointer);
    }

    [Fact]
    public void ADD_MaxOffset508_StackPointerIncremented508()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: add sp, #508
        bus.Write16(0x02000000, 0xb07f);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03007000;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x030071fcu, cpu.Registers.StackPointer);
    }

    [Fact]
    public void SUB_ZeroOffset_StackPointerUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sub sp, #0
        bus.Write16(0x02000000, 0xb080);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03007000;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x03007000u, cpu.Registers.StackPointer);
    }

    [Fact]
    public void SUB_FourOffset_StackPointerDecrementedFour()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sub sp, #4
        bus.Write16(0x02000000, 0xb081);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03007004;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x03007000u, cpu.Registers.StackPointer);
    }

    [Fact]
    public void SUB_MaxOffset_StackPointerDecremented508()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: sub sp, #508
        bus.Write16(0x02000000, 0xb0ff);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x030071fc;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x03007000u, cpu.Registers.StackPointer);
    }
}