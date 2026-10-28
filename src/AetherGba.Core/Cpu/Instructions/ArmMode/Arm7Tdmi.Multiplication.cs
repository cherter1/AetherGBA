using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu;

public sealed partial class Arm7Tdmi
{
    /* MULTIPLICATION INSTRUCTION ENCODINGS

      |..3 ..................2 ..................1 ..................0|
      |1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0|
      |_Cond__|0_0_0_0_0_0|A|S|__Rd___|__Rn___|__Rs___|1_0_0_1|__Rm___| Multiply
      |_Cond__|0_0_0_0_1|U|A|S|__RdHi_|__RdLo_|__Rs___|1_0_0_1|__Rm___| Multiply long
     */

    private void Mul(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rm = (int)instruction & 0xF;
        var rs = (int)(instruction >> 8) & 0xf;
        var rd = (int)(instruction >> 16) & 0xf;

        var multiplierOp = Registers[rs];
        var result = Registers[rm] * multiplierOp; //MUL
        Registers[rd] = result;

        if (setFlags)
        {
            Registers.Cpsr.Negative = (result & 0x80000000) != 0;
            Registers.Cpsr.Zero = result == 0;
        }

        // 1S + mI cycles
        _cycles += GetMultiplierArrayCycles(multiplierOp, false); //I cycles
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, true); //S cycles
    }

    private void Mla(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rm = (int)instruction & 0xF;
        var rs = (int)(instruction >> 8) & 0xf;
        var rn = (int)(instruction >> 12) & 0xf;
        var rd = (int)(instruction >> 16) & 0xf;

        var multiplierOp = Registers[rs];
        var result = (Registers[rm] * multiplierOp) + Registers[rn]; //MLA
        Registers[rd] = result;

        if (setFlags)
        {
            Registers.Cpsr.Negative = (result & 0x80000000) != 0;
            Registers.Cpsr.Zero = result == 0;
        }

        // 1S + (m + 1)I cycles
        _cycles += GetMultiplierArrayCycles(multiplierOp, false) + 1; //I cycles
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, true); //S cycles
    }

    private void Umull(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rm = (int)(instruction & 0xF);
        var rs = (int)((instruction >> 8) & 0xf);
        var rdLo = (int)((instruction >> 12) & 0xf);
        var rdHi = (int)((instruction >> 16) & 0xf);

        var multiplierOp = Registers[rs];
        var res = (ulong)Registers[rm] * multiplierOp; //UMULL

        Registers[rdLo] = (uint)(res & 0xffffffff);
        Registers[rdHi] = (uint)(res >> 32);

        if (setFlags)
        {
            Registers.Cpsr.Negative = ((res >> 32) & 0x80000000) != 0;
            Registers.Cpsr.Zero = res == 0;
        }

        // 1S + (m + 1)I cycles
        _cycles += GetMultiplierArrayCycles(multiplierOp, true) + 1; //I cycles
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, true); //S cycles
    }

    private void Umlal(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rm = (int)(instruction & 0xF);
        var rs = (int)((instruction >> 8) & 0xf);
        var rdLo = (int)((instruction >> 12) & 0xf);
        var rdHi = (int)((instruction >> 16) & 0xf);

        var multiplierOp = Registers[rs];
        var acc = ((ulong)Registers[rdHi] << 32) | Registers[rdLo];

        var res = ((ulong)Registers[rm] * multiplierOp) + acc; //UMLAL

        Registers[rdLo] = (uint)(res & 0xffffffff);
        Registers[rdHi] = (uint)(res >> 32);

        if (setFlags)
        {
            Registers.Cpsr.Negative = ((res >> 32) & 0x80000000) != 0;
            Registers.Cpsr.Zero = res == 0;
        }

        //1S + (m+2)I cycles
        _cycles += GetMultiplierArrayCycles(multiplierOp, true) + 2; //I cycles
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, true); //S cycles
    }

    private void Smull(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rm = (int)(instruction & 0xF);
        var rs = (int)((instruction >> 8) & 0xf);
        var rdLo = (int)((instruction >> 12) & 0xf);
        var rdHi = (int)((instruction >> 16) & 0xf);

        var multiplierOp = Registers[rs];
        var res = (long)(int)Registers[rm] * (int)multiplierOp; //SMULL

        Registers[rdLo] = (uint)(res & 0xffffffff);
        Registers[rdHi] = (uint)(res >> 32);

        if (setFlags)
        {
            Registers.Cpsr.Negative = ((res >> 32) & 0x80000000) != 0;
            Registers.Cpsr.Zero = res == 0;
        }

        // 1S + (m + 1)I cycles
        _cycles += GetMultiplierArrayCycles(multiplierOp, false) + 1; //I cycles
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, true); //S cycles
    }

    private void Smlal(uint instruction)
    {
        var setFlags = (instruction & 0x00100000) != 0; //bit 20
        var rm = (int)(instruction & 0xF);
        var rs = (int)((instruction >> 8) & 0xf);
        var rdLo = (int)((instruction >> 12) & 0xf);
        var rdHi = (int)((instruction >> 16) & 0xf);

        var multiplierOp = Registers[rs];
        long acc = (long)(((ulong)Registers[rdHi] << 32) | Registers[rdLo]);

        var res = ((long)(int)Registers[rm] * (int)multiplierOp) + acc; //SMLAL

        Registers[rdLo] = (uint)(res & 0xffffffff);
        Registers[rdHi] = (uint)(res >> 32);

        if (setFlags)
        {
            Registers.Cpsr.Negative = ((res >> 32) & 0x80000000) != 0;
            Registers.Cpsr.Zero = res == 0;
        }

        //1S + (m+2)I cycles
        _cycles += GetMultiplierArrayCycles(multiplierOp, false) + 2; //I cycles
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, true); //S cycles
    }
}