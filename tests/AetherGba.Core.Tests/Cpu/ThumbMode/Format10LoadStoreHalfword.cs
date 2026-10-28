using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ThumbMode;

public sealed class Format10LoadStoreHalfword
{
    [Fact]
    public void STRH_ZeroOffset_HalfwordStoredAndFlagsUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: strh r0 [r1, #0]
        bus.Write16(0x02000000, 0x8008);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffff5678;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);
        cpu.SetNegative(true);
        cpu.SetOverflow(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x5678u, bus.Read32(0x02000100));

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void STRH_TwoOffset_HalfwordStoredAtR1AddressPlusTwo()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: strh r0 [r1, #2]
        bus.Write16(0x02000000, 0x8048);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffff5678;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x5678u, bus.Read32(0x02000102));
    }

    [Fact]
    public void STRH_MaxOffset62_HalfwordStoredAtR1AddressPlus62()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: strh r0 [r1, #62]
        bus.Write16(0x02000000, 0x87c8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffff5678;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x5678u, bus.Read32(0x0200013e));
    }

    [Fact]
    public void LDRH_ZeroOffset_HalfwordLoadedIntoR0()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldrh r0 [r1, #0]
        bus.Write16(0x02000000, 0x8808);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write32(0x02000100, 0x12345678);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x5678u, cpu.Registers[0]);
    }

    [Fact]
    public void LDRH_FourOffset_HalfwordLoadedIntoR0FromR1AddressPlusFour()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldrh r0 [r1, #4]
        bus.Write16(0x02000000, 0x8888);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write32(0x02000104, 0x12345678);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x5678u, cpu.Registers[0]);
    }

    [Fact]
    public void LDRH_MaxOffset62_HalfwordLoadedIntoR0FromR1AddressPlus62()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldrh r0 [r1, #62]
        bus.Write16(0x02000000, 0x8fc8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write16(0x0200013e, 0x5678);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x5678u, cpu.Registers[0]);
    }
}