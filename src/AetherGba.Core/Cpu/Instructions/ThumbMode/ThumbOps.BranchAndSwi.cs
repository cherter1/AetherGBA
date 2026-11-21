using AetherGba.Core.Common;
using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu.Instructions.ThumbMode;

public static partial class ThumbOps
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

    public static void Bl(Arm7Tdmi cpu, ushort instruction)
    {
        var h = (instruction & 0x800) != 0;
        var offset = instruction & 0x7ff;
        if (h)
        {
            var temp = cpu.Registers.ProgramCounter;
            cpu.Registers.ProgramCounter = cpu.Registers.LinkRegister + (uint)(offset << 1);
            cpu.Registers.ProgramCounter &= ~1u;
            cpu.Registers[14] = temp | 1u;
        }
        else
        {
            var signedOffset = BitUtils.SignExtend(offset << 12, 23);
            cpu.Registers[14] = (uint)(cpu.Registers.ProgramCounter + 2 + signedOffset);
        }

        cpu.Cycles += 3; //TODO: figure cycles later
    }

    public static void B(Arm7Tdmi cpu, ushort instruction)
    {
        var offset = BitUtils.SignExtend((instruction & 0x7FF) << 1, 12);
        cpu.Registers.ProgramCounter = (cpu.Registers.ProgramCounter + 2) + (uint)offset;

        //2S + 1N
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: false);
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true) * 2;
    }

    public static void BCond(Arm7Tdmi cpu, ushort instruction)
    {
        var cond = (instruction >> 8) & 0x0F;
        if (!cpu.ConditionPassed((Condition)cond))
        {
            //1S cycle for unmet conditions
            cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true);
            return;
        }

        var offset = BitUtils.SignExtend((instruction & 0xFF) << 1, 9);

        cpu.Registers.ProgramCounter = (uint)(cpu.Registers.ProgramCounter + 2 + offset);

        //refill pipeline
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: false);
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Halfword, sequential: true) * 2; //2S
    }

    public static void Bx(Arm7Tdmi cpu, ushort instruction)
    {
        var rs = (instruction >> 3) & 0xF;

        var target = rs == 15 ? cpu.Registers[rs] + 2 : cpu.Registers[rs];

        var setThumb = (target & 1) != 0;

        cpu.Registers.Cpsr.ThumbState = setThumb;

        //32 bit align if entering arm else 16 bit aligned
        target &= setThumb ? ~1u : ~3u;
        cpu.Registers.ProgramCounter = target;

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter,
            setThumb ? AccessWidth.Halfword : AccessWidth.Word, sequential: false); //N
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter,
            setThumb ? AccessWidth.Halfword : AccessWidth.Word, sequential: true) * 2; //2S
    }

    public static void Swi(Arm7Tdmi cpu, ushort instruction)
    {
        var comment = instruction & 0xFF;
        //Console.WriteLine("THUMB SWI Enter: comment = " + comment.ToString("X8"));

        cpu.Registers.SetSpsr(CpuMode.Supervisor, cpu.Registers.Cpsr);

        //todo mode
        cpu.Registers.Cpsr.Mode = CpuMode.Supervisor;
        cpu.Registers.Cpsr.IrqDisable = true;
        cpu.Registers.Cpsr.ThumbState = false;

        cpu.Registers[14] = cpu.Registers.ProgramCounter;
        cpu.Registers.ProgramCounter = 0x8; //vector address 0x8

        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: false); //N
        cpu.Cycles += cpu.Bus.GetCpuAccessCycles(cpu.Registers.ProgramCounter, AccessWidth.Word, sequential: true) * 2; //2S cycles
    }

    public static void Illegal(Arm7Tdmi cpu, ushort instruction)
    {
        throw new InvalidOperationException("Illegal instruction");
    }
}