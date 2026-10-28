using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ThumbMode;

public sealed class Format9LoadStoreWithImmOffset
{
    [Fact]
    public void STR_ZeroOffset_FlagsUnchangedAndWordStoredAtR1Address()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: str r0 [r1, #0]
        bus.Write16(0x02000000, 0x6008);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffffffff;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);
        cpu.SetNegative(true);
        cpu.SetZero(true);
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffffffffu, bus.Read32(0x02000100));

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void STR_OffsetFour_WordStoredFourPlusR1Address()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: str r0 [r1, #4]
        bus.Write16(0x02000000, 0x6048);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffffffff;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffffffffu, bus.Read32(0x02000104));
    }

    [Fact]
    public void STR_OffsetMax_WordStored124PlusR1Address()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: str r0 [r1, #124]  @ 0x7c
        bus.Write16(0x02000000, 0x67c8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffffffff;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffffffffu, bus.Read32(0x0200017c));
    }

    [Fact]
    public void LDR_ZeroOffset_WordLoadedIntoR0FromR1()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldr r0 [r1, #0]
        bus.Write16(0x02000000, 0x6808);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x0;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write32(0x02000100, 0xffffffff);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffffffffu, cpu.Registers[0]);
    }

    [Fact]
    public void LDR_FourOffset_WordLoadedToR0FromR1PlusFour()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldr r0 [r1, #4]
        bus.Write16(0x02000000, 0x6848);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x0;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write32(0x02000104, 0xffffffff);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffffffffu, cpu.Registers[0]);
    }

    [Fact]
    public void LDR_MaxOffset124_WordLoadedToR0FromR1Plus124()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldr r0 [r1, #124]  @ 0x7c
        bus.Write16(0x02000000, 0x6fc8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x0;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write32(0x0200017c, 0xffffffff);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xffffffffu, cpu.Registers[0]);
    }

    [Fact]
    public void STRB_ZeroOffset_ByteStoredAtR1Address()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: strb r0 [r1, #0]
        bus.Write16(0x02000000, 0x7008);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x123456AB;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xabu, bus.Read32(0x02000100));
    }

    [Fact]
    public void STRB_FiveOffset_ByteStoredAtR1AddressPlus5()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: strb r0 [r1, #5]
        bus.Write16(0x02000000, 0x7148);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x123456AB;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xabu, bus.Read32(0x02000105));
    }

    [Fact]
    public void STRB_MaxOffset31_ByteStoredAtR1AddressPlus31()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: strb r0 [r1, #31]
        bus.Write16(0x02000000, 0x77c8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x123456AB;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xabu, bus.Read32(0x0200011f));
    }

    [Fact]
    public void LDRB_ZeroOffset_ByteLoadedToR0ZeroExtendedFromR1Address()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldrb r0 [r1, #0]
        bus.Write16(0x02000000, 0x7808);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x0;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write32(0x02000100, 0xffffffab);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xabu, cpu.Registers[0]);
    }

    [Fact]
    public void LDRB_FiveOffset_ByteLoadedToR0ZeroExtendedFromR1AddressPlusFive()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldrb r0 [r1, #5]
        bus.Write16(0x02000000, 0x7948);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x0;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write8(0x02000105, 0xab);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xabu, cpu.Registers[0]);
    }

    [Fact]
    public void LDRB_MaxOffset31_ByteLoadedToR0ZeroExtendedFromR1AddressPlus31()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldrb r0 [r1, #31]
        bus.Write16(0x02000000, 0x7fc8);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x0;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write8(0x0200011f, 0xab);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xabu, cpu.Registers[0]);
    }
}