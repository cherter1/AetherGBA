using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ArmMode;

public sealed class SingleDataTransferTests
{
    [Fact]
    public void STR_StoreWord_StoresValueCorrectly()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mov r11, 0x03000000 @ IW RAM region
        // 0x02000004: mvn r0, #0
        // 0x02000008: str r0, [r11]

        bus.Write32(0x02000000, 0xE3A0B403);
        bus.Write32(0x02000004, 0xE3E00000);
        bus.Write32(0x02000008, 0xE58B0000);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = false;

        //Act
        cpu.Step();
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.Equal(0x03000000u, cpu.Registers[11]);

        var storedValue = bus.Read32(0x03000000);
        Assert.Equal(0xFFFFFFFF, storedValue);
        Assert.Equal(0xFFFFFFFF, cpu.Registers[0]);
    }

    [Fact]
    public void STR_StoreByte_StoresValueCorrectly()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: mov r11, 0x03000000 @ IW RAM region
        // 0x02000004: mvn r0, #0
        // 0x02000008: strb r0, [r11]

        bus.Write32(0x02000000, 0xE3A0B403);
        bus.Write32(0x02000004, 0xE3E00000);
        bus.Write32(0x02000008, 0xE5CB0000);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = false;

        //Act
        cpu.Step();
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.Equal(0x03000000u, cpu.Registers[11]);

        var storedValue = bus.Read8(0x03000000);
        var byteAfterStored = bus.Read8(0x03000001);
        Assert.Equal(0xFF, storedValue);
        Assert.Equal(0, byteAfterStored);
    }

    [Fact]
    public void LDR_LoadWord_LoadsCorrectValue()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        bus.Write32(0x03000000, 0xFFFFFFFF);

        // 0x02000000: mov r11, 0x03000000 @ IW RAM region
        // 0x02000004: ldr r1, [r11]

        bus.Write32(0x02000000, 0xE3A0B403);
        bus.Write32(0x02000004, 0xE59B1000);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = false;

        //Act
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.Equal(0x03000000u, cpu.Registers[11]);
        Assert.Equal(0xFFFFFFFF, cpu.Registers[1]);
    }

    [Fact]
    public void LDR_LoadByte_LoadsCorrectValue()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        bus.Write16(0x03000000, 0xFFFF);

        // 0x02000000: mov r11, 0x03000000 @ IW RAM region
        // 0x02000004: ldrb r1, [r11]

        bus.Write32(0x02000000, 0xE3A0B403);
        bus.Write32(0x02000004, 0xE5DB1000);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = false;

        //Act
        cpu.Step();
        cpu.Step();

        //Assert
        Assert.Equal(0x03000000u, cpu.Registers[11]);
        Assert.Equal(0xFFu, cpu.Registers[1]);
    }

    [Fact]
    public void LDR_LoadCurrentInstruction_Things()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        bus.Write16(0x03000000, 0xFFFF);

        // 0x02000000: mov r11, 0x03000000 @ IW RAM region
        // 0x02000004: ldrb r1, [r11]

        bus.Write32(0x02000000, 0xE3A0B403);
        bus.Write32(0x02000004, 0xE5DB1000);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers.Cpsr.ThumbState = false;

        //Act
        //cpu.Step();
        //cpu.Step();

        //Assert
        //Assert.Equal(0x03000000u, cpu.Registers[11]);
        //Assert.Equal(0xFFu, cpu.Registers[1]);
    }
}