using AetherGba.Core.Common;
using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu;

public sealed partial class Arm7Tdmi
{
    /* BRANCH AND SWI INSTRUCTION ENCODINGS

      |..........1 ..................0|
      |5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0|
      |1_1_0_1_1_1_1_1|____Value8_____| software interrupt
      |1_1_1_0_0|_____Offset11________| unconditional branch
      |1_1_1_1|H|_____Offset11________| long branch with link
      |1_1_0_1|_Cond__|____SOffset8___| conditional branch
      |0_1_0_0_0_1|OP_|H|H|Rs/Hs|Rd/Hd| (h1-7 high bit for Rd, h2-6 h2 high bit for Rs) BX op 11
     */

    private void Bl(ushort instruction)
    {
        var h = (instruction & 0x800) != 0;
        var offset = instruction & 0x7ff;
        if (h)
        {
            var temp = Registers.ProgramCounter;
            Registers.ProgramCounter = Registers.LinkRegister + (uint)(offset << 1);
            Registers.ProgramCounter &= ~1u;
            Registers[14] = temp | 1u;
        }
        else
        {
            var signedOffset = BitUtils.SignExtend(offset << 12, 23);
            Registers[14] = (uint)(Registers.ProgramCounter + 2 + signedOffset);
        }

        _cycles += 3; //TODO: figure cycles later
    }

    private void B(ushort instruction)
    {
        var offset = BitUtils.SignExtend((instruction & 0x7FF) << 1, 12);
        Registers.ProgramCounter = (Registers.ProgramCounter + 2) + (uint)offset;

        //2S + 1N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: false);
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true) * 2;
    }

    private void BCond(ushort instruction)
    {
        var cond = (instruction >> 8) & 0x0F;
        if (!ConditionPassed((Condition)cond))
        {
            //1S cycle for unmet conditions
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
            return;
        }

        var offset = BitUtils.SignExtend((instruction & 0xFF) << 1, 9);

        Registers.ProgramCounter = (uint)(Registers.ProgramCounter + 2 + offset);

        //refill pipeline
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: false);
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Halfword, sequential: true) * 2; //2S
    }

    private void Bx(ushort instruction)
    {
        var rs = (instruction >> 3) & 0xF;

        var target = rs == 15 ? Registers[rs] + 2 : Registers[rs];

        var setThumb = (target & 1) != 0;

        Registers.Cpsr.ThumbState = setThumb;

        //32 bit align if entering arm else 16 bit aligned
        target &= setThumb ? ~1u : ~3u;
        Registers.ProgramCounter = target;

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter,
            setThumb ? AccessWidth.Halfword : AccessWidth.Word, sequential: false); //N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter,
            setThumb ? AccessWidth.Halfword : AccessWidth.Word, sequential: true) * 2; //2S
    }

    private void Swi(ushort instruction)
    {
        var comment = instruction & 0xFF;
        //Console.WriteLine("THUMB SWI Enter: comment = " + comment.ToString("X8"));

        Registers.SetSpsr(CpuMode.Supervisor, Registers.Cpsr);

        //todo mode
        Registers.Cpsr.Mode = CpuMode.Supervisor;
        Registers.Cpsr.IrqDisable = true;
        Registers.Cpsr.ThumbState = false;

        Registers[14] = Registers.ProgramCounter;
        Registers.ProgramCounter = 0x8; //vector address 0x8

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false); //N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true) * 2; //2S cycles
    }

    private void Illegal(ushort instruction)
    {
        throw new InvalidOperationException("Illegal instruction");
    }
}