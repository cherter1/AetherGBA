using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ArmMode.DataProcessing;

public sealed class FiqRegisterBankingTests
{
    [Fact]
    public void MSR_ModeChangesToFiqThenBackToSystem_SystemRegisters8Through12Preserved()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: msr cpsr, 0x11 ; MODE_FIQ
        // 0x02000004: msr spsr, 0x1f ; MODE_SYS
        // 0x02000008: subs pc, pc, #4
        bus.Write32(0x02000000, 0xe329f011);
        bus.Write32(0x02000004, 0xe369f01f);
        bus.Write32(0x02000008, 0xe25ff004);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[8] = 0xD00D;
        cpu.Registers[9] = 0xD00D;
        cpu.Registers[10] = 0xD00D;
        cpu.Registers[11] = 0xD00D;
        cpu.Registers[12] = 0xD00D;

        //Act 1: Switch To Fiq / fill banked reg
        cpu.Step();

        cpu.Registers[8] = 0xFACE;
        cpu.Registers[9] = 0xFACE;
        cpu.Registers[10] = 0xFACE;
        cpu.Registers[11] = 0xFACE;
        cpu.Registers[12] = 0xFACE;

        //Act 2: swap back to system Mode
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.Equal(0xD00Du, cpu.Registers[8]);
        Assert.Equal(0xD00Du, cpu.Registers[9]);
        Assert.Equal(0xD00Du, cpu.Registers[10]);
        Assert.Equal(0xD00Du, cpu.Registers[11]);
        Assert.Equal(0xD00Du, cpu.Registers[12]);
    }
}