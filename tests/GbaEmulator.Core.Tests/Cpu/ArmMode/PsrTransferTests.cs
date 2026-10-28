using GbaEmulator.Core.Cpu;
using GbaEmulator.Core.Memory;
using GbaEmulator.Core.Tests.TestUtils;

namespace GbaEmulator.Core.Tests.Cpu.ArmMode;

public sealed class PsrTransferTests
{
    [Fact]
    public void MRS_ReadCpsr_CopiesFullCpsrCorrectly()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mrs r0, cpsr
        bus.Write32(0x02000000, 0xE10F0000);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = false;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(cpu.Registers.Cpsr.ToUInt32(), cpu.Registers[0]);
    }

    [Fact]
    public void MRS_ReadSpsr_SupervisorMode_CopiesSpsrForCorrectMode()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mrs r0, spsr
        bus.Write32(0x02000000, 0xE14F0000);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.Mode = CpuMode.Supervisor;
        cpu.Registers.SetSpsr(CpuMode.Supervisor, ProgramStatusRegister.FromUInt32(0xF00000F3));

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0xF00000F3, cpu.Registers[0]);
    }

    [Fact]
    public void MRS_ReadSpsr_IrqMode_CopiesSpsrForCorrectMode()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: msr cpsr, r1
        // 0x02000004: mrs r0, spsr
        bus.Write32(0x02000000, 0xE129F001);
        bus.Write32(0x02000004, 0xE14F0000);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.SetSpsr(CpuMode.Irq, ProgramStatusRegister.FromUInt32(0xF00000D2));
        cpu.Registers[1] = 0x000000D2;

        //Act
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.Equal(0xF00000D2, cpu.Registers[0]);
    }

    [Fact]
    public void MSR_WriteCpsr_FromRegisterChangeToUserMode_WritesNewCpsrCorrectly()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mov r0, #144 ; psr same except user mode
        // 0x02000004: msr cpsr_fc, r0
        bus.Write32(0x02000000, 0xE3A00090);
        bus.Write32(0x02000004, 0xE129F000);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = false;

        //Act
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.Equal(CpuMode.User, cpu.Registers.Cpsr.Mode);
        Assert.Equal(0x90u, cpu.Registers.Cpsr.ToUInt32());
    }

    [Fact]
    public void MSR_WriteCpsrFlagsOnly_FromRegister_UpdatesFlags()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mov r0, 0xF0000000 ; set all flags
        // 0x02000004: msr cpsr_flg, r0
        bus.Write32(0x02000000, 0xE3A0020F);
        bus.Write32(0x02000004, 0xE128F000);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = false;

        //Act
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void MSR_WriteCpsrFlagsOnly_FromImmediateValue_UpdatesFlags()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: msr cpsr_flg, 0xF0000000 ; set all flags
        bus.Write32(0x02000000, 0xE328F20F);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = false;

        //Act
        cpu.Step();

        //Assert
        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }
}