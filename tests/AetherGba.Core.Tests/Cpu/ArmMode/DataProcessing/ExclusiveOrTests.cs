using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ArmMode.DataProcessing;

public sealed class ExclusiveOrTests
{
    [Fact]
    public void EOR_NoSBit_ImmediateSecondOperand_CorrectlyComputesXor()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mov r12, 0xFF
        // 0x02000004: eor r12, 0xF0
        bus.Write32(0x02000000, 0xE3A0C0FF);
        bus.Write32(0x02000004, 0xE22CC0F0);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = false;

        //Act
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.Equal(0x0Fu, cpu.Registers[12]);
    }

    [Fact]
    public void EOR_SBit_RegisterSecondOperand_Result31BitSetAndSetsNFlag()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mov r11, 0x00
        // 0x02000004: mov r10, 0x80000000
        // 0x02000008: eors r12, r10, r11
        bus.Write32(0x02000000, 0xE3A0B000);
        bus.Write32(0x02000004, 0xE3A0A102);
        bus.Write32(0x02000008, 0xE03AC00B);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = false;

        //Act
        cpu.Step();
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.Equal(0x80000000u, cpu.Registers[12]);
        Assert.False(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Negative);
    }
}