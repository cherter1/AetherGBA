using AetherGba.Core.Common;
using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu.Instructions.ThumbMode;

public static partial class ThumbOps
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

    public static void LslImm(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var offset = (instruction >> 6) & 0x1F;
        var sourceValue = (int)cpu.Registers[rs];

        var result = (uint)(sourceValue << offset);
        if (offset != 0)
        {
            cpu.Registers.Cpsr.Carry = BitUtils.IsBitSet((uint)sourceValue, 32 - offset);
        }

        cpu.Registers[rd] = result;
        cpu.UpdateNz(result);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void LsrImm(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var offset = (instruction >> 6) & 0x1F;
        var sourceValue = (int)cpu.Registers[rs];

        uint result;
        if (offset == 0)
        {
            result = 0;
            cpu.Registers.Cpsr.Carry = BitUtils.IsBitSet((uint)sourceValue, 31);
        }
        else
        {
            result = (uint)(sourceValue >>> offset);
            cpu.Registers.Cpsr.Carry = BitUtils.IsBitSet((uint)sourceValue, offset - 1);
        }

        cpu.Registers[rd] = result;
        cpu.UpdateNz(result);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void AsrImm(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var offset = (instruction >> 6) & 0x1F;
        var sourceValue = (int)cpu.Registers[rs];

        uint result;
        if (offset == 0)
        {
            var carryOut = BitUtils.IsBitSet((uint)sourceValue, 31);
            result = carryOut ? 0xFFFFFFFF : 0;
            cpu.Registers.Cpsr.Carry = carryOut;
        }
        else
        {
            result = (uint)(sourceValue >> offset);
            cpu.Registers.Cpsr.Carry = BitUtils.IsBitSet((uint)sourceValue, offset - 1);
        }

        cpu.Registers[rd] = result;
        cpu.UpdateNz(result);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }
    #endregion

    #region Format2

    public static void Add(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var immediate = (instruction & 0x400) != 0; //bit 10
        var rnImm = (instruction >> 6) & 0b111;
        var operand2 = immediate ? (uint)rnImm : cpu.Registers[rnImm];

        var result = cpu.Registers[rs] + operand2;

        cpu.UpdateArithmeticFlags(cpu.Registers[rs], operand2, result, false);
        cpu.Registers[rd] = result;

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Sub(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var immediate = (instruction & 0x400) != 0; //bit 10
        var rnImm = (instruction >> 6) & 0b111;
        var operand2 = immediate ? (uint)rnImm : cpu.Registers[rnImm];

        var result = cpu.Registers[rs] - operand2;

        cpu.UpdateArithmeticFlags(cpu.Registers[rs], operand2, result, true);
        cpu.Registers[rd] = result;

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }
    #endregion

    #region Format3

    public static void MovImm(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (uint)(instruction & 0xFF);

        cpu.Registers[rd] = offset;
        cpu.UpdateNz(offset);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void CmpImm(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (uint)(instruction & 0xFF);

        var result = cpu.Registers[rd] - offset;
        cpu.UpdateArithmeticFlags(cpu.Registers[rd], offset, result, subtraction: true);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void AddImm(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (uint)(instruction & 0xFF);

        var result = cpu.Registers[rd] + offset;
        cpu.UpdateArithmeticFlags(cpu.Registers[rd], offset, result, subtraction: false);
        cpu.Registers[rd] = result;

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void SubImm(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = (instruction >> 8) & 0b111;
        var offset = (uint)(instruction & 0xFF);

        var result = cpu.Registers[rd] - offset;
        cpu.UpdateArithmeticFlags(cpu.Registers[rd], offset, result, subtraction: true);
        cpu.Registers[rd] = result;

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }
    #endregion

    #region Format4

    public static void And(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = cpu.Registers[rd] & cpu.Registers[rs];
        cpu.UpdateNz(result);
        cpu.Registers[rd] = result;

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Eor(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = cpu.Registers[rd] ^ cpu.Registers[rs];
        cpu.UpdateNz(result);
        cpu.Registers[rd] = result;

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Lsl(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var shiftAmount = (int)(cpu.Registers[rs] & 0xFF);
        var result = cpu.ShiftLeft(cpu.Registers[rd], shiftAmount, out bool carryOut);
        cpu.Registers.Cpsr.Carry = carryOut;
        cpu.UpdateNz(result);
        cpu.Registers[rd] = result;

        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Lsr(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var shiftAmount = (int)(cpu.Registers[rs] & 0xFF);
        var result = cpu.ShiftRightLogical(cpu.Registers[rd], shiftAmount, true, out var carryOut);
        cpu.Registers.Cpsr.Carry = carryOut;
        cpu.UpdateNz(result);
        cpu.Registers[rd] = result;

        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Asr(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var shiftAmount = (int)(cpu.Registers[rs] & 0xFF);
        var result = cpu.ShiftRightArithmetic(cpu.Registers[rd], shiftAmount, true, out var carryOut);
        cpu.Registers.Cpsr.Carry = carryOut;
        cpu.UpdateNz(result);
        cpu.Registers[rd] = result;

        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Adc(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var cy = cpu.Registers.Cpsr.Carry ? 1u : 0u;

        var wide = (ulong)cpu.Registers[rd] + cpu.Registers[rs] + cy;
        var result = (uint)wide;
        cpu.UpdateArithmeticFlags(cpu.Registers[rd], cpu.Registers[rs], result, subtraction: false);
        cpu.Registers.Cpsr.Carry = wide >> 32 != 0;
        cpu.Registers[rd] = result;

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Sbc(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;
        var cy = cpu.Registers.Cpsr.Carry ? 1u : 0u;

        var longResult = (ulong)cpu.Registers[rd] - cpu.Registers[rs] + cy - 1u;
        var result = (uint)longResult;
        cpu.UpdateArithmeticFlags(cpu.Registers[rd], cpu.Registers[rs], result, subtraction: true);
        cpu.Registers.Cpsr.Carry = (long)longResult >= 0;
        cpu.Registers[rd] = result;

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Ror(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var shiftAmount = (int)(cpu.Registers[rs] & 0xFF);
        var result = cpu.RotateRight(cpu.Registers[rd], shiftAmount, true, out var carryOut);
        cpu.UpdateNz(result);
        if (shiftAmount != 0)
        {
            cpu.Registers.Cpsr.Carry = carryOut;
        }
        cpu.Registers[rd] = result;

        cpu.Cycles++; //I
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Tst(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = cpu.Registers[rd] & cpu.Registers[rs];
        cpu.UpdateNz(result);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Neg(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = 0 - cpu.Registers[rs];
        cpu.UpdateArithmeticFlags(0, cpu.Registers[rs], result, true);
        cpu.Registers[rd] = result;

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Cmp(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = cpu.Registers[rd] - cpu.Registers[rs];
        cpu.UpdateArithmeticFlags(cpu.Registers[rd], cpu.Registers[rs], result, subtraction: true);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Cmn(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = cpu.Registers[rd] + cpu.Registers[rs];
        cpu.UpdateArithmeticFlags(cpu.Registers[rd], cpu.Registers[rs], result, subtraction: false);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Orr(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = cpu.Registers[rd] | cpu.Registers[rs];
        cpu.Registers[rd] = result;
        cpu.UpdateNz(result);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Mul(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var multiplierOperand = cpu.Registers[rd];
        var result = multiplierOperand * cpu.Registers[rs];
        cpu.Registers[rd] = result;
        cpu.UpdateNz(result);
        cpu.Registers.Cpsr.Carry = false;

        cpu.Cycles += Arm7Tdmi.GetMultiplierArrayCycles(multiplierOperand, false); //mI cycles
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Bic(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = cpu.Registers[rd] & ~cpu.Registers[rs];
        cpu.Registers[rd] = result;
        cpu.UpdateNz(result);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void Mvn(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = instruction & 0b111;
        var rs = (instruction >> 3) & 0b111;

        var result = ~cpu.Registers[rs];
        cpu.Registers[rd] = result;
        cpu.UpdateNz(result);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }
    #endregion

    #region Format5

    public static void AddHiReg(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = ((instruction >> 4) & 0x8) | (instruction & 0b111);
        var rs = (instruction >> 3) & 0xF;
        var source = rs == 15 ? cpu.Registers[rs] + 2 : cpu.Registers[rs];
        var destOperand = rd == 15 ? cpu.Registers[rd] + 2 : cpu.Registers[rd];

        cpu.Registers[rd] = destOperand + source;
        if (rd == 15)
        {
            cpu.Registers[rd] &= ~1u;

            //refill pipeline adds 1S and 1N
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void CmpHiReg(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = ((instruction >> 4) & 0x8) | (instruction & 0b111);
        var rs = (instruction >> 3) & 0xF;
        var source = rs == 15 ? cpu.Registers[rs] + 2 : cpu.Registers[rs];
        var destOperand = rd == 15 ? cpu.Registers[rd] + 2 : cpu.Registers[rd];

        var result = destOperand - source;
        cpu.UpdateArithmeticFlags(destOperand, source, result, subtraction: true);

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }

    public static void MovHiReg(Arm7Tdmi cpu, ushort instruction)
    {
        var rd = ((instruction >> 4) & 0x8) | (instruction & 0b111);
        var rs = (instruction >> 3) & 0xF;
        var source = rs == 15 ? cpu.Registers[rs] + 2 : cpu.Registers[rs];

        cpu.Registers[rd] = source;
        if (rd == 15)
        {
            cpu.Registers[rd] &= ~1u;

            //refill pipeline adds 1S and 1N
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
    }
    #endregion

    #region Format12

    public static void AddWithPcOrSp(Arm7Tdmi cpu, ushort instruction)
    {
        var source = (instruction & 0x800) != 0; //bit 11
        var immediate = (instruction & 0xFF) << 2;
        var rd = (instruction >> 8) & 0b111;
        var operand1 = source
            ? cpu.Registers.StackPointer
            : (cpu.Registers.ProgramCounter + 2) & ~3u;

        cpu.Registers[rd] = operand1 + (uint)immediate;

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }
    #endregion

    #region Format13

    public static void AddSubOffsetSp(Arm7Tdmi cpu, ushort instruction)
    {
        var signed = (instruction & 0x80) != 0; //bit 7
        var imm = (instruction & 0x7F) << 2;

        if (signed)
        {
            cpu.Registers[13] = cpu.Registers.StackPointer - (uint)imm;
        }
        else
        {
            cpu.Registers[13] = cpu.Registers.StackPointer + (uint)imm;
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true); //S
    }
    #endregion
}