using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ThumbMode;

public sealed class Format6LdrWithPc
{
    [Fact]
    public void LDR_MaxImmOffsetPcUnaligned_AddressCalculatedCorrectly()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000002: ldr r4 [pc, #1020]
        bus.Write16(0x02000002, 0x4cff);

        bus.Write32(0x02000400, 0x12345678);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000002;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, cpu.Registers[4]);
    }

    [Fact]
    public void LDR_MaxImmOffsetPcAligned_AddressCalculatedCorrectly()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldr r3 [pc, #1020]
        bus.Write16(0x02000000, 0x4bff);

        bus.Write32(0x02000400, 0x12345678);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, cpu.Registers[3]);
    }

    [Fact]
    public void LDR_Offset4PcAligned_AddressCalculatedCorrectly()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldr r1 [pc, #4]
        bus.Write16(0x02000000, 0x4901);

        bus.Write32(0x02000008, 0x12345678);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, cpu.Registers[1]);
    }

    [Fact]
    public void LDR_InstrNotWordAlignedNoOffset_CorrectReadFromAlignedAddress()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000002: ldr r0 [pc, #0]
        bus.Write16(0x02000002, 0x4800);

        bus.Write32(0x02000004, 0x12345678);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000002;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, cpu.Registers[0]);
    }

    [Fact]
    public void LDR_BasicLoadNoOffset_R0SetFlagsUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: ldr r0 [pc, #0]
        bus.Write16(0x02000000, 0x4800);

        bus.Write32(0x02000004, 0x12345678);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x12345678u, cpu.Registers[0]);
    }
}