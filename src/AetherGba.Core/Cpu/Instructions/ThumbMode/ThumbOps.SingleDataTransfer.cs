using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu.Instructions.ThumbMode;

public static partial class ThumbOps
{
    /* SINGLE DATA TRANSFER INSTRUCTION ENCODINGS

      |..........1 ..................0|
      |5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0|
      |0_1_0_0_1|_Rd__|____Word8______| PC-Relative load (LDR with PC)
      |0_1_0_1|L|B|0|_Ro__|_Rb__|_Rd__| Load/Store with reg offset
      |0_1_1|B|L|_Offset5_|_Rb__|_Rd__| Load/Store with immediate offset
      |1_0_0_1|L|_Rd__|____Word8______| SP-relative Load/Store
     */

    #region Format6

    public static void LdrPc(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (instruction & 0xFF) << 2;
        var pc = (cpu.Registers.ProgramCounter + 2) & ~3u;
        var address = pc + (uint)offset;
        cpu.Registers[rd] = cpu.Bus.Read32(address);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(address, AccessWidth.Word, sequential: false); //N
        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }
    #endregion

    #region Format7

    public static void Str(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = cpu.Registers[rb] + cpu.Registers[ro];

        cpu.Bus.Write32(effectiveAddress, cpu.Registers[rd]); //STR
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    public static void Strb(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = cpu.Registers[rb] + cpu.Registers[ro];

        cpu.Bus.Write8(effectiveAddress, (byte)cpu.Registers[rd]); //STRB
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Byte, sequential: false); //N
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    public static void Ldr(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = cpu.Registers[rb] + cpu.Registers[ro];

        cpu.Registers[rd] = cpu.Bus.Read32(effectiveAddress); //LDR

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N
        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }

    public static void Ldrb(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = cpu.Registers[rb] + cpu.Registers[ro];

        cpu.Registers[rd] = cpu.Bus.Read8(effectiveAddress); //LDRB

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Byte, sequential: false); //N
        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }

    #endregion

    #region Format9

    public static void StrImm(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var offset = ((instruction >> 6) & 0x1F) << 2;
        var effectiveAddress = cpu.Registers[rb] + (uint)offset;

        cpu.Bus.Write32(effectiveAddress, cpu.Registers[rd]);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    public static void LdrImm(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var offset = ((instruction >> 6) & 0x1F) << 2;
        var effectiveAddress = cpu.Registers[rb] + (uint)offset;

        cpu.Registers[rd] = cpu.Bus.Read32(effectiveAddress);

        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }

    public static void StrbImm(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var offset = (instruction >> 6) & 0x1F;
        var effectiveAddress = cpu.Registers[rb] + (uint)offset;

        cpu.Bus.Write8(effectiveAddress, (byte)cpu.Registers[rd]);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Byte, sequential: false); //N
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    public static void LdrbImm(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var offset = (instruction >> 6) & 0x1F;
        var effectiveAddress = cpu.Registers[rb] + (uint)offset;

        cpu.Registers[rd] = cpu.Bus.Read8(effectiveAddress);

        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Byte, sequential: false); //N
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }
    #endregion

    #region Format11

    public static void StrWithSp(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (instruction & 0xFF) << 2;
        var effectiveAddress = cpu.Registers.StackPointer + (uint)offset;

        cpu.Bus.Write32(effectiveAddress, cpu.Registers[rd]);
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    public static void LdrWithSp(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (instruction & 0xFF) << 2;
        var effectiveAddress = cpu.Registers.StackPointer + (uint)offset;

        cpu.Registers[rd] = cpu.Bus.Read32(effectiveAddress);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N
        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }
    #endregion
}