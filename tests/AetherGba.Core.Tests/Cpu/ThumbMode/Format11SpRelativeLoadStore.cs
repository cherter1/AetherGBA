using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ThumbMode;

public sealed class Format11SpRelativeLoadStore
{
    [Fact]
    public void STR_ZeroOffset_R0WordStoredAtSpAndFlagsUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: str r0 [sp, #0]
        bus.Write16(0x02000000, 0x9000);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03007000;
        cpu.Registers[0] = 0x12345678;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);
        cpu.SetNegative(true);
        cpu.SetOverflow(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, bus.Read32(0x03007000));

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void STR_FourOffset_R0WordStoredAtSpPlusFour()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: str r0 [sp, #4]
        bus.Write16(0x02000000, 0x9001);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03007000;
        cpu.Registers[0] = 0x12345678;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, bus.Read32(0x03007004));
    }

    [Fact]
    public void STR_MaxOffset1020_R7WordStoredAtSpPlus1020()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: str r7 [sp, #4]
        bus.Write16(0x02000000, 0x97ff);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03007000;
        cpu.Registers[7] = 0x12345678;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, bus.Read32(0x030073fc));
    }

    [Fact]
    public void LDR_ZeroOffset_WordAtSpLoadedIntoR0()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldr r0 [sp, #0]
        bus.Write16(0x02000000, 0x9800);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03007000;
        bus.Write32(0x03007000, 0x12345678);
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, cpu.Registers[0]);
    }

    [Fact]
    public void LDR_FourOffset_WordAtSpPlusFourLoadedIntoR2()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldr r2 [sp, #4]
        bus.Write16(0x02000000, 0x9a01);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03007000;
        bus.Write32(0x03007004, 0x12345678);
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, cpu.Registers[2]);
    }

    [Fact]
    public void LDR_MaxOffset1020_WordAtSpPlus1020LoadedIntoR5()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldr r5 [sp, #1020]
        bus.Write16(0x02000000, 0x9dff);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03007000;
        bus.Write32(0x030073fc, 0x12345678);
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, cpu.Registers[5]);
    }
}