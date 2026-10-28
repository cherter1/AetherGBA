using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu;

public sealed partial class Arm7Tdmi
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

    private void LdrPc(ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (instruction & 0xFF) << 2;
        var pc = (Registers.ProgramCounter + 2) & ~3u;
        var address = pc + (uint)offset;
        Registers[rd] = _bus.Read32(address);

        _cycles += _bus.GetCpuAccessCycles(address, AccessWidth.Word, sequential: false); //N
        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }
    #endregion

    #region Format7

    private void Str(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = Registers[rb] + Registers[ro];

        _bus.Write32(effectiveAddress, Registers[rd]); //STR
        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    private void Strb(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = Registers[rb] + Registers[ro];

        _bus.Write8(effectiveAddress, (byte)Registers[rd]); //STRB
        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Byte, sequential: false); //N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    private void Ldr(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = Registers[rb] + Registers[ro];

        Registers[rd] = _bus.Read32(effectiveAddress); //LDR

        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N
        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }

    private void Ldrb(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var ro = (instruction >> 6) & 0b111;
        var effectiveAddress = Registers[rb] + Registers[ro];

        Registers[rd] = _bus.Read8(effectiveAddress); //LDRB

        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Byte, sequential: false); //N
        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }

    #endregion

    #region Format9

    private void StrImm(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var offset = ((instruction >> 6) & 0x1F) << 2;
        var effectiveAddress = Registers[rb] + (uint)offset;

        _bus.Write32(effectiveAddress, Registers[rd]);

        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    private void LdrImm(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var offset = ((instruction >> 6) & 0x1F) << 2;
        var effectiveAddress = Registers[rb] + (uint)offset;

        Registers[rd] = _bus.Read32(effectiveAddress);

        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }

    private void StrbImm(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var offset = (instruction >> 6) & 0x1F;
        var effectiveAddress = Registers[rb] + (uint)offset;

        _bus.Write8(effectiveAddress, (byte)Registers[rd]);

        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Byte, sequential: false); //N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    private void LdrbImm(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rb = (instruction >> 3) & 0b111;
        var offset = (instruction >> 6) & 0x1F;
        var effectiveAddress = Registers[rb] + (uint)offset;

        Registers[rd] = _bus.Read8(effectiveAddress);

        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Byte, sequential: false); //N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }
    #endregion

    #region Format11

    private void StrWithSp(ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (instruction & 0xFF) << 2;
        var effectiveAddress = Registers.StackPointer + (uint)offset;

        _bus.Write32(effectiveAddress, Registers[rd]);
        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: false); //N
    }

    private void LdrWithSp(ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (instruction & 0xFF) << 2;
        var effectiveAddress = Registers.StackPointer + (uint)offset;

        Registers[rd] = _bus.Read32(effectiveAddress);

        _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N
        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }
    #endregion
}