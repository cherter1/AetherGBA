using AetherGba.Core.Common;
using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu;

public sealed partial class Arm7Tdmi
{
    /* ALU INSTRUCTION ENCODINGS

      |..........1 ..................0|
      |5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0|
      |0_0_0|1_1|I|O|_Rni_|_Rs__|_Rd__| ADD, SUB (lo reg or 3 bit imm value)
      |0_0_0|OP_|_Offset5_|_Rs__|_Rd__| LSL, LSR, ASR (lo reg 5 bit shifter imm value)
      |0_0_1|OP_|_Rd__|__Offset8______| MOV, CMP, ADD, SUB (8b imm)
      |0_1_0_0_0_0|__OP___|_Rs__|_Rd__| ALU OPS (Lo reg pair)
      |0_1_0_0_0_1|OP_|H|H|Rs/Hs|Rd/Hd| (h1-7 high bit for Rd, h2-6 h2 high bit for Rs) - ADD, CMP, MOV (lo and hi reg or hi reg pair)
      |1_0_1_0|S|_Rd__|____Word8______| (S = pc or sp) - Load address
      |1_0_1_1_0_0_0_0|S|__SWord7_____| (S = sign flag) - add offset to stack pointer
     */

    #region Format1

    private void LslImm(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var offset = (instruction >> 6) & 0x1F;
        var sourceValue = (int)Registers[rs];

        var result = (uint)(sourceValue << offset);
        if (offset != 0)
        {
            Registers.Cpsr.Carry = BitUtils.IsBitSet((uint)sourceValue, 32 - offset);
        }

        Registers[rd] = result;
        UpdateNz(result);

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void LsrImm(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var offset = (instruction >> 6) & 0x1F;
        var sourceValue = (int)Registers[rs];

        uint result;
        if (offset == 0)
        {
            result = 0;
            Registers.Cpsr.Carry = BitUtils.IsBitSet((uint)sourceValue, 31);
        }
        else
        {
            result = (uint)(sourceValue >>> offset);
            Registers.Cpsr.Carry = BitUtils.IsBitSet((uint)sourceValue, offset - 1);
        }

        Registers[rd] = result;
        UpdateNz(result);

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void AsrImm(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var offset = (instruction >> 6) & 0x1F;
        var sourceValue = (int)Registers[rs];

        uint result;
        if (offset == 0)
        {
            var carryOut = BitUtils.IsBitSet((uint)sourceValue, 31);
            result = carryOut ? 0xFFFFFFFF : 0;
            Registers.Cpsr.Carry = carryOut;
        }
        else
        {
            result = (uint)(sourceValue >> offset);
            Registers.Cpsr.Carry = BitUtils.IsBitSet((uint)sourceValue, offset - 1);
        }

        Registers[rd] = result;
        UpdateNz(result);

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }
    #endregion

    #region Format2

    private void Add(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var immediate = (instruction & 0x400) != 0; //bit 10
        var rnImm = (instruction >> 6) & 0b111;
        var operand2 = immediate ? (uint)rnImm : Registers[rnImm];

        var result = Registers[rs] + operand2;

        UpdateArithmeticFlags(Registers[rs], operand2, result, false);
        Registers[rd] = result;

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Sub(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var immediate = (instruction & 0x400) != 0; //bit 10
        var rnImm = (instruction >> 6) & 0b111;
        var operand2 = immediate ? (uint)rnImm : Registers[rnImm];

        var result = Registers[rs] - operand2;

        UpdateArithmeticFlags(Registers[rs], operand2, result, true);
        Registers[rd] = result;

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }
    #endregion

    #region Format3

    private void MovImm(ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (uint)(instruction & 0xFF);

        Registers[rd] = offset;
        UpdateNz(offset);

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void CmpImm(ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (uint)(instruction & 0xFF);

        var result = Registers[rd] - offset;
        UpdateArithmeticFlags(Registers[rd], offset, result, subtraction: true);

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void AddImm(ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (uint)(instruction & 0xFF);

        var result = Registers[rd] + offset;
        UpdateArithmeticFlags(Registers[rd], offset, result, subtraction: false);
        Registers[rd] = result;

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void SubImm(ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (uint)(instruction & 0xFF);

        var result = Registers[rd] - offset;
        UpdateArithmeticFlags(Registers[rd], offset, result, subtraction: true);
        Registers[rd] = result;

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }
    #endregion

    #region Format4

    private void And(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = Registers[rd] & Registers[rs];
        UpdateNz(result);
        Registers[rd] = result;

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Eor(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = Registers[rd] ^ Registers[rs];
        UpdateNz(result);
        Registers[rd] = result;

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Lsl(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var shiftAmount = (int)(Registers[rs] & 0xFF);
        var result = this.ShiftLeft(Registers[rd], shiftAmount, out bool carryOut);
        Registers.Cpsr.Carry = carryOut;
        UpdateNz(result);
        Registers[rd] = result;

        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Lsr(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var shiftAmount = (int)(Registers[rs] & 0xFF);
        var result = this.ShiftRightLogical(Registers[rd], shiftAmount, true, out var carryOut);
        Registers.Cpsr.Carry = carryOut;
        UpdateNz(result);
        Registers[rd] = result;

        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Asr(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var shiftAmount = (int)(Registers[rs] & 0xFF);
        var result = this.ShiftRightArithmetic(Registers[rd], shiftAmount, true, out var carryOut);
        Registers.Cpsr.Carry = carryOut;
        UpdateNz(result);
        Registers[rd] = result;

        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Adc(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var cy = Registers.Cpsr.Carry ? 1u : 0u;

        var wide = (ulong)Registers[rd] + Registers[rs] + cy;
        var result = (uint)wide;
        UpdateArithmeticFlags(Registers[rd], Registers[rs], result, subtraction: false);
        Registers.Cpsr.Carry = wide >> 32 != 0;
        Registers[rd] = result;

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Sbc(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var cy = Registers.Cpsr.Carry ? 1u : 0u;

        var longResult = (ulong)Registers[rd] - Registers[rs] + cy - 1u;
        var result = (uint)longResult;
        UpdateArithmeticFlags(Registers[rd], Registers[rs], result, subtraction: true);
        Registers.Cpsr.Carry = (long)longResult >= 0;
        Registers[rd] = result;

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Ror(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var shiftAmount = (int)(Registers[rs] & 0xFF);
        var result = this.RotateRight(Registers[rd], shiftAmount, true, out var carryOut);
        UpdateNz(result);
        if (shiftAmount != 0)
        {
            Registers.Cpsr.Carry = carryOut;
        }
        Registers[rd] = result;

        _cycles++; //I
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Tst(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = Registers[rd] & Registers[rs];
        UpdateNz(result);

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Neg(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = 0 - Registers[rs];
        UpdateArithmeticFlags(0, Registers[rs], result, true);
        Registers[rd] = result;

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Cmp(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = Registers[rd] - Registers[rs];
        UpdateArithmeticFlags(Registers[rd], Registers[rs], result, subtraction: true);

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Cmn(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = Registers[rd] + Registers[rs];
        UpdateArithmeticFlags(Registers[rd], Registers[rs], result, subtraction: false);

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Orr(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = Registers[rd] | Registers[rs];
        Registers[rd] = result;
        UpdateNz(result);

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Mul(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var multiplierOperand = Registers[rd];
        var result = multiplierOperand * Registers[rs];
        Registers[rd] = result;
        UpdateNz(result);
        Registers.Cpsr.Carry = false;

        _cycles += GetMultiplierArrayCycles(multiplierOperand, false); //mI cycles
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Bic(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = Registers[rd] & ~Registers[rs];
        Registers[rd] = result;
        UpdateNz(result);

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void Mvn(ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = ~Registers[rs];
        Registers[rd] = result;
        UpdateNz(result);

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }
    #endregion

    #region Format5

    private void AddHiReg(ushort instruction)
    {
        var rd = ((instruction >> 4) & 0x8) | (instruction & 0b111);
        var rs = (instruction >> 3) & 0xF;
        var source = rs == 15 ? Registers[rs] + 2 : Registers[rs];
        var destOperand = rd == 15 ? Registers[rd] + 2 : Registers[rd];

        Registers[rd] = destOperand + source;
        if (rd == 15)
        {
            Registers[rd] &= ~1u;

            //refill pipeline adds 1S and 1N
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void CmpHiReg(ushort instruction)
    {
        var rd = ((instruction >> 4) & 0x8) | (instruction & 0b111);
        var rs = (instruction >> 3) & 0xF;
        var source = rs == 15 ? Registers[rs] + 2 : Registers[rs];
        var destOperand = rd == 15 ? Registers[rd] + 2 : Registers[rd];

        var result = destOperand - source;
        UpdateArithmeticFlags(destOperand, source, result, subtraction: true);

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    private void MovHiReg(ushort instruction)
    {
        var rd = ((instruction >> 4) & 0x8) | (instruction & 0b111);
        var rs = (instruction >> 3) & 0xF;
        var source = rs == 15 ? Registers[rs] + 2 : Registers[rs];

        Registers[rd] = source;
        if (rd == 15)
        {
            Registers[rd] &= ~1u;

            //refill pipeline adds 1S and 1N
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }
    #endregion

    #region Format12

    private void AddWithPcOrSp(ushort instruction)
    {
        var source = (instruction & 0x800) != 0; //bit 11
        var immediate = (instruction & 0xFF) << 2;
        var rd = (instruction >> 8) & 0b111;
        var operand1 = source
            ? Registers.StackPointer
            : (Registers.ProgramCounter + 2) & ~3u;

        Registers[rd] = operand1 + (uint)immediate;

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }
    #endregion

    #region Format13

    private void AddSubOffsetSp(ushort instruction)
    {
        var signed = (instruction & 0x80) != 0; //bit 7
        var imm = (instruction & 0x7F) << 2;

        if (signed)
        {
            Registers[13] = Registers.StackPointer - (uint)imm;
        }
        else
        {
            Registers[13] = Registers.StackPointer + (uint)imm;
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }
    #endregion
}