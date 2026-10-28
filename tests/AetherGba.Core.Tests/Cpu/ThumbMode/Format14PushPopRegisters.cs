using AetherGba.Core.Cpu;
using AetherGba.Core.Memory;
using AetherGba.Core.Tests.TestUtils;

namespace AetherGba.Core.Tests.Cpu.ThumbMode;

public sealed class Format14PushPopRegisters
{
    [Fact]
    public void PUSH_ThreeSparseRegisters_StackPointerDecremted12BytesAndFlagsUnchanged()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: push {r0, r1, r3}
        bus.Write16(0x02000000, 0xb40b);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03008000;
        cpu.Registers[0] = 0x11111111;
        cpu.Registers[1] = 0x22222222;
        cpu.Registers[3] = 0x33333333;
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetCarry(true);
        cpu.SetNegative(true);
        cpu.SetOverflow(true);
        cpu.SetZero(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x03007ff4u, cpu.Registers.StackPointer);
        Assert.Equal(0x11111111u, bus.Read32(0x03007ff4));
        Assert.Equal(0x22222222u, bus.Read32(0x03007ff8));
        Assert.Equal(0x33333333u, bus.Read32(0x03007ffc));

        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
        Assert.True(cpu.Registers.Cpsr.Zero);
    }

    [Fact]
    public void PUSH_LrOnly_StackPointerDecrements4BytesPutsValuesAtNewSpAddress()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: push {lr}
        bus.Write16(0x02000000, 0xb500);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[14] = 0x02000101;
        cpu.Registers[13] = 0x03008000;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x03007ffcu, cpu.Registers.StackPointer);
        Assert.Equal(0x02000101u, bus.Read32(0x03007ffc));
    }

    [Fact]
    public void PUSH_LrWithMultipleRegisters_StoresRegistersAtDecrementingSp()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: push {r4, r5, r6, r7, lr}
        bus.Write16(0x02000000, 0xb5f0);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03008000;
        cpu.Registers[4] = 0x44444444;
        cpu.Registers[5] = 0x55555555;
        cpu.Registers[6] = 0x66666666;
        cpu.Registers[7] = 0x77777777;
        cpu.Registers[14] = 0x02000101;
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x03007fecu, cpu.Registers.StackPointer);
        Assert.Equal(0x44444444u, bus.Read32(0x03007fec));
        Assert.Equal(0x55555555u, bus.Read32(0x03007ff0));
        Assert.Equal(0x66666666u, bus.Read32(0x03007ff4));
        Assert.Equal(0x77777777u, bus.Read32(0x03007ff8));
        Assert.Equal(0x02000101u, bus.Read32(0x03007ffc));
    }

    [Fact]
    public void POP_ThreeSparseRegisters_FlagsUnchangedStackPointerIncrementedAndRegistersFilled()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: pop {r0, r5, r6}
        bus.Write16(0x02000000, 0xbc61);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03007ff4;
        bus.Write32(0x03007ff4, 0x11111111);
        bus.Write32(0x03007ff8, 0x55555555);
        bus.Write32(0x03007ffc, 0x66666666);
        cpu.Registers.Cpsr.ThumbState = true;
        cpu.SetZero(true);
        cpu.SetCarry(true);
        cpu.SetNegative(true);
        cpu.SetOverflow(true);

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x03008000u, cpu.Registers.StackPointer);
        Assert.Equal(0x11111111u, cpu.Registers[0]);
        Assert.Equal(0x55555555u, cpu.Registers[5]);
        Assert.Equal(0x66666666u, cpu.Registers[6]);

        Assert.True(cpu.Registers.Cpsr.Zero);
        Assert.True(cpu.Registers.Cpsr.Carry);
        Assert.True(cpu.Registers.Cpsr.Negative);
        Assert.True(cpu.Registers.Cpsr.Overflow);
    }

    [Fact]
    public void POP_PcOnly_SpIncrementedAndPcLoadedThumbStatePreserved()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: pop {pc}
        bus.Write16(0x02000000, 0xbd00);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03007ffc;
        bus.Write32(0x03007ffc, 0x02000101);
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x03008000u, cpu.Registers.StackPointer);
        Assert.Equal(0x02000100u, cpu.Registers.ProgramCounter);

        Assert.True(cpu.Registers.Cpsr.ThumbState);
    }

    [Fact]
    public void POP_PcWithMultipleRegisters_SpIncrementedAndRegistersLoadedWithStackValues()
    {
        //Arrange
        (Arm7Tdmi cpu, GbaBus bus) = CpuUtilities.CreateCpu();

        // 0x02000000: pop {r4, r5, r6, r7, pc}
        bus.Write16(0x02000000, 0xbdf0);

        cpu.Reset();
        cpu.Registers.ProgramCounter = 0x02000000;
        cpu.Registers[13] = 0x03007fec;
        bus.Write32(0x03007fec, 0x44444444);
        bus.Write32(0x03007ff0, 0x55555555);
        bus.Write32(0x03007ff4, 0x66666666);
        bus.Write32(0x03007ff8, 0x77777777);
        bus.Write32(0x03007ffc, 0x02000101);
        cpu.Registers.Cpsr.ThumbState = true;

        //Act
        cpu.Step();

        //Assert
        Assert.Equal(0x03008000u, cpu.Registers.StackPointer);
        Assert.Equal(0x02000100u, cpu.Registers.ProgramCounter);
        Assert.Equal(0x44444444u, cpu.Registers[4]);
        Assert.Equal(0x55555555u, cpu.Registers[5]);
        Assert.Equal(0x66666666u, cpu.Registers[6]);
        Assert.Equal(0x77777777u, cpu.Registers[7]);

        Assert.True(cpu.Registers.Cpsr.ThumbState);
    }
}