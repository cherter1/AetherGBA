using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu.Instructions.ArmMode;

public static partial class ArmOps
{
    /* DATA PROCESSING INSTRUCTION ENCODINGS

      |..3 ..................2 ..................1 ..................0|
      |1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0|
      |_Cond__|0_0_0|___Op__|S|__Rn___|__Rd___|__Shift__|Typ|0|__Rm___| DataProc
      |_Cond__|0_0_0|___Op__|S|__Rn___|__Rd___|__Rs___|0|Typ|1|__Rm___| DataProc
      |_Cond__|0_0_1|___Op__|S|__Rn___|__Rd___|_Shift_|___Immediate___| DataProc
     */

    public static void And(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? cpu.Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : cpu.Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : cpu.Registers[rn];
        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out var carryOut);

        var result = op1 & op2; //AND
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.UpdateNz(result);
            cpu.Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void AndImm(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var op2 = cpu.DecodeImmediateOperand(instruction, out var carryOut);

        var result = op1 & op2; //AND
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.UpdateNz(result);
            cpu.Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Eor(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? cpu.Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : cpu.Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : cpu.Registers[rn];
        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out var carryOut);

        var result = op1 ^ op2; //EOR
        cpu.Registers[rd] = result;
        if (setFlags)
        {
            cpu.UpdateNz(result);
            cpu.Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void EorImm(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var op2 = cpu.DecodeImmediateOperand(instruction, out var carryOut);

        var result = op1 ^ op2; //EOR
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.UpdateNz(result);
            cpu.Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Sub(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? cpu.Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : cpu.Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : cpu.Registers[rn];
        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out _);

        var result = op1 - op2; //SUB
        cpu.Registers[rd] = result;
        if (setFlags)
        {
            if (rd == 15)
            {
                cpu.Registers.Cpsr = cpu.Registers.GetSpsr();
            }
            else
            {
                cpu.UpdateArithmeticFlags(op1, op2, result, subtraction: true);
            }
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void SubImm(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var op2 = cpu.DecodeImmediateOperand(instruction, out _);

        var result = op1 - op2; //SUB
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            if (rd == 15)
            {
                cpu.Registers.Cpsr = cpu.Registers.GetSpsr();
            }
            else
            {
                cpu.UpdateArithmeticFlags(op1, op2, result, subtraction: true);
            }
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Rsb(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? cpu.Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : cpu.Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : cpu.Registers[rn];
        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out _);

        var result = op2 - op1; //RSB
        cpu.Registers[rd] = result;

        if (setFlags) cpu.UpdateArithmeticFlags(op2, op1, result, subtraction: true);

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void RsbImm(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var op2 = cpu.DecodeImmediateOperand(instruction, out _);

        var result = op2 - op1; //RSB
        cpu.Registers[rd] = result;

        if (setFlags) cpu.UpdateArithmeticFlags(op2, op1, result, subtraction: true);

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Add(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? cpu.Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : cpu.Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : cpu.Registers[rn];
        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out _);

        var result = op1 + op2; //ADD
        cpu.Registers[rd] = result;

        if (setFlags) cpu.UpdateArithmeticFlags(op1, op2, result, subtraction: false);

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void AddImm(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var op2 = cpu.DecodeImmediateOperand(instruction, out _);

        var result = op1 + op2; //ADD
        cpu.Registers[rd] = result;

        if (setFlags) cpu.UpdateArithmeticFlags(op1, op2, result, subtraction: false);

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Adc(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? cpu.Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : cpu.Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : cpu.Registers[rn];
        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out _);
        var cy = cpu.Registers.Cpsr.Carry ? 1u : 0u;

        var wide = (ulong)op1 + op2 + cy; //ADC
        var result = (uint)wide;
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.UpdateArithmeticFlags(op1, op2, result, subtraction: false);
            //Set Carry after to set it correctly
            cpu.Registers.Cpsr.Carry = wide >> 32 != 0;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void AdcImm(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var op2 = cpu.DecodeImmediateOperand(instruction, out _);
        var cy = cpu.Registers.Cpsr.Carry ? 1u : 0u;

        var wide = (ulong)op1 + op2 + cy; //ADC
        var result = (uint)wide;
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.UpdateArithmeticFlags(op1, op2, result, subtraction: false);
            //Set Carry after to set it correctly
            cpu.Registers.Cpsr.Carry = wide >> 32 != 0;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Sbc(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? cpu.Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : cpu.Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : cpu.Registers[rn];
        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out _);
        var cy = cpu.Registers.Cpsr.Carry ? 1u : 0u;

        var wide = (ulong)op1 - op2 + cy - 1u; //SBC
        var result = (uint)wide;
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.UpdateArithmeticFlags(op1, op2, result, subtraction: true);
            //Set Carry after to set it correctly
            cpu.Registers.Cpsr.Carry = (long)wide >= 0;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void SbcImm(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var op2 = cpu.DecodeImmediateOperand(instruction, out _);
        var cy = cpu.Registers.Cpsr.Carry ? 1u : 0u;

        var wide = (ulong)op1 - op2 + cy - 1u; //SBC
        var result = (uint)wide;
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.UpdateArithmeticFlags(op1, op2, result, subtraction: true);
            //Set Carry after to set it correctly
            cpu.Registers.Cpsr.Carry = (long)wide >= 0;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Rsc(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? cpu.Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : cpu.Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : cpu.Registers[rn];
        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out _);
        var cy = cpu.Registers.Cpsr.Carry ? 1u : 0u;

        var wide = (ulong)op2 - op1 + cy - 1u; //RSC
        var result = (uint)wide;
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.UpdateArithmeticFlags(op2, op1, result, subtraction: true);
            //Set Carry after to set it correctly
            cpu.Registers.Cpsr.Carry = (long)wide >= 0;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void RscImm(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var op2 = cpu.DecodeImmediateOperand(instruction, out _);
        var cy = cpu.Registers.Cpsr.Carry ? 1u : 0u;

        var wide = (ulong)op2 - op1 + cy - 1u; //RSC
        var result = (uint)wide;
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.UpdateArithmeticFlags(op2, op1, result, subtraction: true);
            //Set Carry after to set it correctly
            cpu.Registers.Cpsr.Carry = (long)wide >= 0;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Tst(Arm7Tdmi cpu, uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? cpu.Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : cpu.Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : cpu.Registers[rn];
        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out var carryOut);

        var result = op1 & op2; //TST

        cpu.UpdateNz(result);
        cpu.Registers.Cpsr.Carry = carryOut;

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void TstImm(Arm7Tdmi cpu, uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var op2 = cpu.DecodeImmediateOperand(instruction, out var carryOut);

        var result = op1 & op2; //TST

        cpu.UpdateNz(result);
        cpu.Registers.Cpsr.Carry = carryOut;

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Teq(Arm7Tdmi cpu, uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? cpu.Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : cpu.Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : cpu.Registers[rn];
        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out var carryOut);

        var result = op1 ^ op2; //TEQ

        cpu.UpdateNz(result);
        cpu.Registers.Cpsr.Carry = carryOut;

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void TeqImm(Arm7Tdmi cpu, uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var op2 = cpu.DecodeImmediateOperand(instruction, out var carryOut);

        var result = op1 ^ op2; //TEQ

        cpu.UpdateNz(result);
        cpu.Registers.Cpsr.Carry = carryOut;

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Cmp(Arm7Tdmi cpu, uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? cpu.Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : cpu.Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : cpu.Registers[rn];
        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out _);

        var result = op1 - op2; //CMP

        cpu.UpdateArithmeticFlags(op1, op2, result, subtraction: true);

        if (rd == 15)
        {
            var oldMode = cpu.Registers.Cpsr.Mode;
            if (oldMode != CpuMode.User && oldMode != CpuMode.System)
            {
                cpu.Registers.Cpsr = cpu.Registers.GetSpsr();
            }

            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void CmpImm(Arm7Tdmi cpu, uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var op2 = cpu.DecodeImmediateOperand(instruction, out _);

        var result = op1 - op2; //CMP

        cpu.UpdateArithmeticFlags(op1, op2, result, subtraction: true);

        if (rd == 15)
        {
            var oldMode = cpu.Registers.Cpsr.Mode;
            if (oldMode != CpuMode.User && oldMode != CpuMode.System)
            {
                cpu.Registers.Cpsr = cpu.Registers.GetSpsr();
            }

            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Cmn(Arm7Tdmi cpu, uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? cpu.Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : cpu.Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : cpu.Registers[rn];
        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out _);

        var result = op1 + op2; //CMN

        cpu.UpdateArithmeticFlags(op1, op2, result, subtraction: false);

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void CmnImm(Arm7Tdmi cpu, uint instruction)
    {
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var op2 = cpu.DecodeImmediateOperand(instruction, out _);

        var result = op1 + op2; //CMN

        cpu.UpdateArithmeticFlags(op1, op2, result, subtraction: false);

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Orr(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? cpu.Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : cpu.Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : cpu.Registers[rn];
        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out var carryOut);

        var result = op1 | op2; //ORR
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.UpdateNz(result);
            cpu.Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void OrrImm(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var op2 = cpu.DecodeImmediateOperand(instruction, out var carryOut);

        var result = op1 | op2; //ORR
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.UpdateNz(result);
            cpu.Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Mov(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rd = (int)((instruction >> 12) & 0xf);

        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out var carryOut);

        cpu.Registers[rd] = op2; //MOV

        if (setFlags)
        {
            cpu.UpdateNz(op2);
            cpu.Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            if (setFlags)
            {
                cpu.Registers.Cpsr = cpu.Registers.GetSpsr();
                //TODO mode
            }
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void MovImm(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rd = (int)((instruction >> 12) & 0xf);

        var op2 = cpu.DecodeImmediateOperand(instruction, out var carryOut);

        cpu.Registers[rd] = op2; //MOV

        if (setFlags)
        {
            cpu.UpdateNz(op2);
            cpu.Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            if (setFlags)
            {
                cpu.Registers.Cpsr = cpu.Registers.GetSpsr();
                //TODO mode
            }
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Bic(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? (instruction & 0x10) != 0 //bit 4 set
                ? cpu.Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : cpu.Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : cpu.Registers[rn];
        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out var carryOut);

        var result = op1 & ~op2; //BIC
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.UpdateNz(result);
            cpu.Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void BicImm(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rn = (int)((instruction >> 16) & 0xf);
        var rd = (int)((instruction >> 12) & 0xf);

        var op1 = rn == 15
            ? cpu.Registers.ProgramCounter + 4
            : cpu.Registers[rn];
        var op2 = cpu.DecodeImmediateOperand(instruction, out var carryOut);

        var result = op1 & ~op2; //BIC
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.UpdateNz(result);
            cpu.Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void Mvn(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rd = (int)((instruction >> 12) & 0xf);

        var op2 = cpu.ComputeShiftedRegisterOperand(instruction, out var carryOut);

        var result = ~op2; //MVN
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.UpdateNz(result);
            cpu.Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }
        //if shift by register value amount, add I cycle
        if ((instruction & 0x10) != 0) cpu.Cycles += 1; //I cycle

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    public static void MvnImm(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rd = (int)((instruction >> 12) & 0xf);

        var op2 = cpu.DecodeImmediateOperand(instruction, out var carryOut);

        var result = ~op2; //MVN
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.UpdateNz(result);
            cpu.Registers.Cpsr.Carry = carryOut;
        }

        if (rd == 15)
        {
            //simulates pipeline flush adding extra cycle S cycle and N cycle
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
        }

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }
}