using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ThumbMode;

public sealed class Format7LoadStoreRegOffset
{
    [Fact]
    public void STR_StoreWordWithOffset_FlagsUnchangedAndValueStored()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: str r0 [r1, r2]
        bus.Write16(0x02000000, 0x5088);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x12345678;
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
        Assert.Equal(0x12345678u, bus.Read32(0x02000104));

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void STR_StoreWordWithZeroOffset_ValueStored()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: str r0 [r1, r2]
        bus.Write16(0x02000000, 0x5088);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0x12345678;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers[2] = 0;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, bus.Read32(0x02000100));
    }

    [Fact]
    public void STRB_OffsetSet_LowByteOnlyStored()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: strb r0 [r1, r2]
        bus.Write16(0x02000000, 0x5488);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffffffab;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers[2] = 4;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xabu, bus.Read32(0x02000104));
    }

    [Fact]
    public void LDR_ZeroOffset_LoadsWordIntoRegister()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldr r0 [r1, r2]
        bus.Write16(0x02000000, 0x5888);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffffffff;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers[2] = 0;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write32(0x02000100, 0x12345678);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, cpu.Registers[0]);
    }

    [Fact]
    public void LDRB_OffsetSet_LoadsByteZeroExtendedIntoRegister()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldrb r0 [r1, r2]
        bus.Write16(0x02000000, 0x5c88);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[0] = 0xffffffff;
        cpu.Registers[1] = 0x02000100;
        cpu.Registers[2] = 4;
        cpu.Registers.Cpsr.ThumbState = true;
        bus.Write32(0x02000104, 0xffffffab);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xabu, cpu.Registers[0]);
    }
}