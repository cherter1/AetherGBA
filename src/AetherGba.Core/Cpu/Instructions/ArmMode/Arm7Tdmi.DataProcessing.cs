using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu;

public sealed partial class Arm7Tdmi
{
    /* DATA PROCESSING INSTRUCTION ENCODINGS

      |..3 ..................2 ..................1 ..................0|
      |1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0|
      |_Cond__|0_0_0|___Op__|S|__Rn___|__Rd___|__Shift__|Typ|0|__Rm___| DataProc
      |_Cond__|0_0_0|___Op__|S|__Rn___|__Rd___|__Rs___|0|Typ|1|__Rm___| DataProc
      |_Cond__|0_0_1|___Op__|S|__Rn___|__Rd___|_Shift_|___Immediate___| DataProc
     */

    private void And(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : Registers[rn];
        var op2 = ComputeShiftedRegisterOperand(instruction, out var carryOut);

        var result = op1 & op2; //AND
        Registers[rd] = result;

        if (setFlags)
        {
            UpdateNz(result);
            Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void AndImm(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var op2 = DecodeImmediateOperand(instruction, out var carryOut);

        var result = op1 & op2; //AND
        Registers[rd] = result;

        if (setFlags)
        {
            UpdateNz(result);
            Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Eor(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : Registers[rn];
        var op2 = ComputeShiftedRegisterOperand(instruction, out var carryOut);

        var result = op1 ^ op2; //EOR
        Registers[rd] = result;
        if (setFlags)
        {
            UpdateNz(result);
            Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void EorImm(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var op2 = DecodeImmediateOperand(instruction, out var carryOut);

        var result = op1 ^ op2; //EOR
        Registers[rd] = result;

        if (setFlags)
        {
            UpdateNz(result);
            Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Sub(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : Registers[rn];
        var op2 = ComputeShiftedRegisterOperand(instruction, out _);

        var result = op1 - op2; //SUB
        Registers[rd] = result;
        if (setFlags)
        {
            if (rd == 15)
            {
                Registers.Cpsr = Registers.GetSpsr();
            }
            else
            {
                UpdateArithmeticFlags(op1, op2, result, subtraction: true);
            }
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void SubImm(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var op2 = DecodeImmediateOperand(instruction, out _);

        var result = op1 - op2; //SUB
        Registers[rd] = result;

        if (setFlags)
        {
            if (rd == 15)
            {
                Registers.Cpsr = Registers.GetSpsr();
            }
            else
            {
                UpdateArithmeticFlags(op1, op2, result, subtraction: true);
            }
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Rsb(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : Registers[rn];
        var op2 = ComputeShiftedRegisterOperand(instruction, out _);

        var result = op2 - op1; //RSB
        Registers[rd] = result;

        if (setFlags) UpdateArithmeticFlags(op2, op1, result, subtraction: true);

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void RsbImm(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var op2 = DecodeImmediateOperand(instruction, out _);

        var result = op2 - op1; //RSB
        Registers[rd] = result;

        if (setFlags) UpdateArithmeticFlags(op2, op1, result, subtraction: true);

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Add(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : Registers[rn];
        var op2 = ComputeShiftedRegisterOperand(instruction, out _);

        var result = op1 + op2; //ADD
        Registers[rd] = result;

        if (setFlags) UpdateArithmeticFlags(op1, op2, result, subtraction: false);

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void AddImm(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var op2 = DecodeImmediateOperand(instruction, out _);

        var result = op1 + op2; //ADD
        Registers[rd] = result;

        if (setFlags) UpdateArithmeticFlags(op1, op2, result, subtraction: false);

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Adc(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : Registers[rn];
        var op2 = ComputeShiftedRegisterOperand(instruction, out _);
        var cy = Registers.Cpsr.Carry ? 1u : 0u;

        var wide = (ulong)op1 + op2 + cy; //ADC
        var result = (uint)wide;
        Registers[rd] = result;

        if (setFlags)
        {
            UpdateArithmeticFlags(op1, op2, result, subtraction: false);
            //Set Carry after to set it correctly
            Registers.Cpsr.Carry = wide >> 32 != 0;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void AdcImm(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var op2 = DecodeImmediateOperand(instruction, out _);
        var cy = Registers.Cpsr.Carry ? 1u : 0u;

        var wide = (ulong)op1 + op2 + cy; //ADC
        var result = (uint)wide;
        Registers[rd] = result;

        if (setFlags)
        {
            UpdateArithmeticFlags(op1, op2, result, subtraction: false);
            //Set Carry after to set it correctly
            Registers.Cpsr.Carry = wide >> 32 != 0;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Sbc(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : Registers[rn];
        var op2 = ComputeShiftedRegisterOperand(instruction, out _);
        var cy = Registers.Cpsr.Carry ? 1u : 0u;

        var wide = (ulong)op1 - op2 + cy - 1u; //SBC
        var result = (uint)wide;
        Registers[rd] = result;

        if (setFlags)
        {
            UpdateArithmeticFlags(op1, op2, result, subtraction: true);
            //Set Carry after to set it correctly
            Registers.Cpsr.Carry = (long)wide >= 0;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void SbcImm(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var op2 = DecodeImmediateOperand(instruction, out _);
        var cy = Registers.Cpsr.Carry ? 1u : 0u;

        var wide = (ulong)op1 - op2 + cy - 1u; //SBC
        var result = (uint)wide;
        Registers[rd] = result;

        if (setFlags)
        {
            UpdateArithmeticFlags(op1, op2, result, subtraction: true);
            //Set Carry after to set it correctly
            Registers.Cpsr.Carry = (long)wide >= 0;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Rsc(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : Registers[rn];
        var op2 = ComputeShiftedRegisterOperand(instruction, out _);
        var cy = Registers.Cpsr.Carry ? 1u : 0u;

        var wide = (ulong)op2 - op1 + cy - 1u; //RSC
        var result = (uint)wide;
        Registers[rd] = result;

        if (setFlags)
        {
            UpdateArithmeticFlags(op2, op1, result, subtraction: true);
            //Set Carry after to set it correctly
            Registers.Cpsr.Carry = (long)wide >= 0;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void RscImm(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var op2 = DecodeImmediateOperand(instruction, out _);
        var cy = Registers.Cpsr.Carry ? 1u : 0u;

        var wide = (ulong)op2 - op1 + cy - 1u; //RSC
        var result = (uint)wide;
        Registers[rd] = result;

        if (setFlags)
        {
            UpdateArithmeticFlags(op2, op1, result, subtraction: true);
            //Set Carry after to set it correctly
            Registers.Cpsr.Carry = (long)wide >= 0;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Tst(uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : Registers[rn];
        var op2 = ComputeShiftedRegisterOperand(instruction, out var carryOut);

        var result = op1 & op2; //TST

        UpdateNz(result);
        Registers.Cpsr.Carry = carryOut;

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void TstImm(uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var op2 = DecodeImmediateOperand(instruction, out var carryOut);

        var result = op1 & op2; //TST

        UpdateNz(result);
        Registers.Cpsr.Carry = carryOut;

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Teq(uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : Registers[rn];
        var op2 = ComputeShiftedRegisterOperand(instruction, out var carryOut);

        var result = op1 ^ op2; //TEQ

        UpdateNz(result);
        Registers.Cpsr.Carry = carryOut;

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void TeqImm(uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var op2 = DecodeImmediateOperand(instruction, out var carryOut);

        var result = op1 ^ op2; //TEQ

        UpdateNz(result);
        Registers.Cpsr.Carry = carryOut;

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Cmp(uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : Registers[rn];
        var op2 = ComputeShiftedRegisterOperand(instruction, out _);

        var result = op1 - op2; //CMP

        UpdateArithmeticFlags(op1, op2, result, subtraction: true);

        if (rd == 15)
        {
            var oldMode = Registers.Cpsr.Mode;
            if (oldMode != CpuMode.User && oldMode != CpuMode.System)
            {
                Registers.Cpsr = Registers.GetSpsr();
            }

            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void CmpImm(uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var op2 = DecodeImmediateOperand(instruction, out _);

        var result = op1 - op2; //CMP

        UpdateArithmeticFlags(op1, op2, result, subtraction: true);

        if (rd == 15)
        {
            var oldMode = Registers.Cpsr.Mode;
            if (oldMode != CpuMode.User && oldMode != CpuMode.System)
            {
                Registers.Cpsr = Registers.GetSpsr();
            }

            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Cmn(uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : Registers[rn];
        var op2 = ComputeShiftedRegisterOperand(instruction, out _);

        var result = op1 + op2; //CMN

        UpdateArithmeticFlags(op1, op2, result, subtraction: false);

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void CmnImm(uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var op2 = DecodeImmediateOperand(instruction, out _);

        var result = op1 + op2; //CMN

        UpdateArithmeticFlags(op1, op2, result, subtraction: false);

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Orr(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : Registers[rn];
        var op2 = ComputeShiftedRegisterOperand(instruction, out var carryOut);

        var result = op1 | op2; //ORR
        Registers[rd] = result;

        if (setFlags)
        {
            UpdateNz(result);
            Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void OrrImm(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var op2 = DecodeImmediateOperand(instruction, out var carryOut);

        var result = op1 | op2; //ORR
        Registers[rd] = result;

        if (setFlags)
        {
            UpdateNz(result);
            Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Mov(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rd = (int)((instruction >> 12) & 0xf);

        var op2 = ComputeShiftedRegisterOperand(instruction, out var carryOut);

        Registers[rd] = op2; //MOV

        if (setFlags)
        {
            UpdateNz(op2);
            Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            if (setFlags)
            {
                Registers.Cpsr = Registers.GetSpsr();
                //TODO mode
            }
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void MovImm(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rd = (int)((instruction >> 12) & 0xf);

        var op2 = DecodeImmediateOperand(instruction, out var carryOut);

        Registers[rd] = op2; //MOV

        if (setFlags)
        {
            UpdateNz(op2);
            Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            if (setFlags)
            {
                Registers.Cpsr = Registers.GetSpsr();
                //TODO mode
            }
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Bic(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : Registers[rn];
        var op2 = ComputeShiftedRegisterOperand(instruction, out var carryOut);

        var result = op1 & ~op2; //BIC
        Registers[rd] = result;

        if (setFlags)
        {
            UpdateNz(result);
            Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void BicImm(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? Registers.ProgramCounter + 4
            : Registers[rn];
        var op2 = DecodeImmediateOperand(instruction, out var carryOut);

        var result = op1 & ~op2; //BIC
        Registers[rd] = result;

        if (setFlags)
        {
            UpdateNz(result);
            Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Mvn(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rd = (int)((instruction >> 12) & 0xf);

        var op2 = ComputeShiftedRegisterOperand(instruction, out var carryOut);

        var result = ~op2; //MVN
        Registers[rd] = result;

        if (setFlags)
        {
            UpdateNz(result);
            Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) _cycles += 1; //I cycle

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void MvnImm(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rd = (int)((instruction >> 12) & 0xf);

        var op2 = DecodeImmediateOperand(instruction, out var carryOut);

        var result = ~op2; //MVN
        Registers[rd] = result;

        if (setFlags)
        {
            UpdateNz(result);
            Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }
}