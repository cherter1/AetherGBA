using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu.Instructions.ArmMode;

public static partial class ArmOps
{
    /* MULTIPLICATION INSTRUCTION ENCODINGS

      |..3 ..................2 ..................1 ..................0|
      |1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0|
      |_Cond__|0_0_0_0_0_0|A|S|__Rd___|__Rn___|__Rs___|1_0_0_1|__Rm___| Multiply
      |_Cond__|0_0_0_0_1|U|A|S|__RdHi_|__RdLo_|__Rs___|1_0_0_1|__Rm___| Multiply long
     */

    public static void Mul(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rm = (int)instruction & 0xF;
        var rs = (int)(instruction >> 8) & 0xf;
        var rd = (int)(instruction >> 16) & 0xf;

        var multiplierOp = cpu.Registers[rs];
        var result = cpu.Registers[rm] * multiplierOp; //MUL
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.Registers.Cpsr.Negative = (result & 0x80000000) != 0;
            cpu.Registers.Cpsr.Zero = result == 0;
        }

        // 1S + mI cycles
        cpu.Cycles += Arm7Tdmi.GetMultiplierArrayCycles(multiplierOp, false); //I cycles
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, true); //S cycles
    }

    public static void Mla(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rm = (int)instruction & 0xF;
        var rs = (int)(instruction >> 8) & 0xf;
        var rn = (int)(instruction >> 12) & 0xf;
        var rd = (int)(instruction >> 16) & 0xf;

        var multiplierOp = cpu.Registers[rs];
        var result = (cpu.Registers[rm] * multiplierOp) + cpu.Registers[rn]; //MLA
        cpu.Registers[rd] = result;

        if (setFlags)
        {
            cpu.Registers.Cpsr.Negative = (result & 0x80000000) != 0;
            cpu.Registers.Cpsr.Zero = result == 0;
        }

        // 1S + (m + 1)I cycles
        cpu.Cycles += Arm7Tdmi.GetMultiplierArrayCycles(multiplierOp, false) + 1; //I cycles
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, true); //S cycles
    }

    public static void Umull(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rm = (int)(instruction & 0xF);
        var rs = (int)((instruction >> 8) & 0xf);
        var rdLo = (int)((instruction >> 12) & 0xf);
        var rdHi = (int)((instruction >> 16) & 0xf);

        var multiplierOp = cpu.Registers[rs];
        var res = (ulong)cpu.Registers[rm] * multiplierOp; //UMULL

        cpu.Registers[rdLo] = (uint)(res & 0xffffffff);
        cpu.Registers[rdHi] = (uint)(res >> 32);

        if (setFlags)
        {
            cpu.Registers.Cpsr.Negative = ((res >> 32) & 0x80000000) != 0;
            cpu.Registers.Cpsr.Zero = res == 0;
        }

        // 1S + (m + 1)I cycles
        cpu.Cycles += Arm7Tdmi.GetMultiplierArrayCycles(multiplierOp, true) + 1; //I cycles
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, true); //S cycles
    }

    public static void Umlal(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rm = (int)(instruction & 0xF);
        var rs = (int)((instruction >> 8) & 0xf);
        var rdLo = (int)((instruction >> 12) & 0xf);
        var rdHi = (int)((instruction >> 16) & 0xf);

        var multiplierOp = cpu.Registers[rs];
        var acc = ((ulong)cpu.Registers[rdHi] << 32) | cpu.Registers[rdLo];

        var res = ((ulong)cpu.Registers[rm] * multiplierOp) + acc; //UMLAL

        cpu.Registers[rdLo] = (uint)(res & 0xffffffff);
        cpu.Registers[rdHi] = (uint)(res >> 32);

        if (setFlags)
        {
            cpu.Registers.Cpsr.Negative = ((res >> 32) & 0x80000000) != 0;
            cpu.Registers.Cpsr.Zero = res == 0;
        }

        //1S + (m+2)I cycles
        cpu.Cycles += Arm7Tdmi.GetMultiplierArrayCycles(multiplierOp, true) + 2; //I cycles
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, true); //S cycles
    }

    public static void Smull(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rm = (int)(instruction & 0xF);
        var rs = (int)((instruction >> 8) & 0xf);
        var rdLo = (int)((instruction >> 12) & 0xf);
        var rdHi = (int)((instruction >> 16) & 0xf);

        var multiplierOp = cpu.Registers[rs];
        var res = (long)(int)cpu.Registers[rm] * (int)multiplierOp; //SMULL

        cpu.Registers[rdLo] = (uint)(res & 0xffffffff);
        cpu.Registers[rdHi] = (uint)(res >> 32);

        if (setFlags)
        {
            cpu.Registers.Cpsr.Negative = ((res >> 32) & 0x80000000) != 0;
            cpu.Registers.Cpsr.Zero = res == 0;
        }

        // 1S + (m + 1)I cycles
        cpu.Cycles += Arm7Tdmi.GetMultiplierArrayCycles(multiplierOp, false) + 1; //I cycles
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, true); //S cycles
    }

    public static void Smlal(Arm7Tdmi cpu, uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rm = (int)(instruction & 0xF);
        var rs = (int)((instruction >> 8) & 0xf);
        var rdLo = (int)((instruction >> 12) & 0xf);
        var rdHi = (int)((instruction >> 16) & 0xf);

        var multiplierOp = cpu.Registers[rs];
        long acc = (long)(((ulong)cpu.Registers[rdHi] << 32) | cpu.Registers[rdLo]);

        var res = ((long)(int)cpu.Registers[rm] * (int)multiplierOp) + acc; //SMLAL

        cpu.Registers[rdLo] = (uint)(res & 0xffffffff);
        cpu.Registers[rdHi] = (uint)(res >> 32);

        if (setFlags)
        {
            cpu.Registers.Cpsr.Negative = ((res >> 32) & 0x80000000) != 0;
            cpu.Registers.Cpsr.Zero = res == 0;
        }

        //1S + (m+2)I cycles
        cpu.Cycles += Arm7Tdmi.GetMultiplierArrayCycles(multiplierOp, false) + 2; //I cycles
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, true); //S cycles
    }
}