using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ThumbMode;

public sealed class Format8LoadStoreSeByteHalfword
{
    [Fact]
    public void STRH_WithOffset_FlagsUnchangedAndLowHalfOfValueStored()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: strh r0 [r1, r2]
        bus.Write16(0x02000000, 0x5288);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffff5678;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers[2] = 4;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);
        cpu.SetNegative(true);
        cpu.SetZero(true);
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x5678u, bus.Read32(0x02000104));

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void LDRH_ZeroOffset_ValueLoadedIntoRegister()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldrh r0 [r1, r2]
        bus.Write16(0x02000000, 0x5a88);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffffffff;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers[2] = 0;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write32(0x02000100, 0x8fff);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x8fffu, cpu.Registers[0]);
    }

    [Fact]
    public void LDSB_NegativeNumber_ValueLoadedIntoRegisterOneExtended()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldsb r0 [r1, r2]
        bus.Write16(0x02000000, 0x5688);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffffffff;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers[2] = 4;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write32(0x02000104, 0x80);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffffff80u, cpu.Registers[0]);
    }

    [Fact]
    public void LDSB_PositiveNumber_ValueIntoRegisterZeroExtended()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldsb r0 [r1, r2]
        bus.Write16(0x02000000, 0x5688);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffffffff;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers[2] = 4;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write32(0x02000104, 0x7f);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x7fu, cpu.Registers[0]);
    }

    [Fact]
    public void LDSH_NegativeNumber_ValueIntoRegisterOneExtended()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldsh r0 [r1, r2]
        bus.Write16(0x02000000, 0x5e88);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffffffff;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers[2] = 0;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write32(0x02000100, 0x8000);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffff8000u, cpu.Registers[0]);
    }

    [Fact]
    public void LDSH_PositiveNumber_ValueIntoRegisterZeroExtended()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldsh r0 [r1, r2]
        bus.Write16(0x02000000, 0x5e88);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffffffff;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers[2] = 4;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write32(0x02000104, 0x7fff);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x7fffu, cpu.Registers[0]);
    }
}