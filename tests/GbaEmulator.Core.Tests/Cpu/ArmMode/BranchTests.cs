using GbaEmulator.Core.Cpu;
using GbaEmulator.Core.Memory;
using GbaEmulator.Core.Tests.TestUtils;

namespace GbaEmulator.Core.Tests.Cpu.ArmMode;

public sealed class BranchTests
{
    [Fact]
    public void Bx_Arm_Exchange_SetsThumbStateAndBranches()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        //ARM: BX r0
        bus.Write32(0x02000000, 0xE12FFF10);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = false;

        cpu.Registers[0] = 0x02000009;

        //Act
        cpu.Step();

        //Assert
        Assert.True(cpu.Registers.Cpsr.ThumbState);
        Assert.Equal(0x02000008u, cpu.Registers.ProgramCounter);
    }

    [Fact]
    public void Bx_Thumb_Exchange_ClearsThumbStateAndBranches()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        //Thumb: BX r0
        bus.Write16(0x02000000, 0x4700);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = true;

        cpu.Registers[0] = 0x02000008;

        //Act
        cpu.Step();

        //Assert
        Assert.False(cpu.Registers.Cpsr.ThumbState);
        Assert.Equal(0x02000008u, cpu.Registers.ProgramCounter);
    }

    [Fact]
    public void Bx_Arm_NoExchange_BranchesAndStaysInArm()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        //ARM: BX r0
        bus.Write32(0x02000000, 0xE12FFF10);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = false;

        cpu.Registers[0] = 0x02000008;

        //Act
        cpu.Step();

        //Assert
        Assert.False(cpu.Registers.Cpsr.ThumbState);
        Assert.Equal(0x02000008u, cpu.Registers.ProgramCounter);
    }

    [Fact]
    public void B_Arm_Forward_BranchesToCorrectTarget()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mov r12, #53
        // 0x02000004: b 0x0200000C
        // 0x02000008: mov r12, #99 @ should be skipped
        // 0x0200000C: mov r12, #54

        bus.Write32(0x02000000, 0xE3A0C035);
        bus.Write32(0x02000004, 0xEA000000);
        bus.Write32(0x02000008, 0xE3A0C063);
        bus.Write32(0x0200000C, 0xE3A0C036);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = false;

        //Act
        cpu.Step();
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.Equal(54u, cpu.Registers[12]);
        Assert.Equal(0x02000010u, cpu.Registers.ProgramCounter);
    }

    [Fact]
    public void B_Arm_Backward_BranchesToCorrectTarget()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mov r12, #54
        // 0x02000004: b 0x02000000
        // 0x02000008: mov r12, #99 @ should be skipped

        bus.Write32(0x02000000, 0xE3A0C036);
        bus.Write32(0x02000004, 0xEAFFFFFD);
        bus.Write32(0x02000008, 0xE3A0C063);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000004;
        cpu.Registers.Cpsr.ThumbState = false;

        //Act
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.Equal(54u, cpu.Registers[12]);
        Assert.Equal(0x02000004u, cpu.Registers.ProgramCounter);
    }

    [Fact]
    public void BL_Arm_BackwardLink_BranchesToCorrectTargetAndSetsLinkRegister()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mov r12, #57
        // 0x02000004: mov pc, lr
        // 0x02000008: mov r12, #99
        // 0x0200000C: bl 0x02000000

        bus.Write32(0x02000000, 0xE3A0C039);
        bus.Write32(0x02000004, 0xE1A0F00E);
        bus.Write32(0x02000008, 0xE3A0C063);
        bus.Write32(0x0200000C, 0xEBFFFFFB);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000008;
        cpu.Registers.Cpsr.ThumbState = false;

        //Act
        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.Equal(57u, cpu.Registers[12]);
        Assert.Equal(0x02000010u, cpu.Registers.ProgramCounter);
    }
}