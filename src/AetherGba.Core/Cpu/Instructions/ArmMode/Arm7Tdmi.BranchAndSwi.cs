using AetherGba.Core.Common;
using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu;

public sealed partial class Arm7Tdmi
{
    /* BRANCH and SWI INSTRUCTION ENCODINGS

      |..3 ..................2 ..................1 ..................0|
      |1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0|
      |_Cond__|1_0_1|L|___________________Offset______________________| B,BL
      |_Cond__|0_0_0_1_0_0_1_0_1_1_1_1_1_1_1_1_1_1_1_1|0_0|L|1|__Rn___| BX
      |_Cond__|1_1_1_1|_____________Ignored_by_Processor______________| SWI
     */

    private void B(uint instruction)
    {
        var offset = BitUtils.SignExtend((int)(instruction & 0x00FFFFFF) << 2, 26);
        var pc = Registers.ProgramCounter;

        //docs say +8 but only do +4 because StepArm() function also adds 4
        Registers.ProgramCounter = (uint)(pc + 4 + offset);

        //2S + 1N cycles basically 1S and FlushingPipeline
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false); //1N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true) * 2; //2S
    }

    private void Bl(uint instruction)
    {
        var offset = BitUtils.SignExtend((int)(instruction & 0x00FFFFFF) << 2, 26);
        var pc = Registers.ProgramCounter;

        Registers[14] = pc;

        //docs say +8 but only do +4 because StepArm() function also adds 4
        Registers.ProgramCounter = (uint)(pc + 4 + offset);

        //2S + 1N cycles basically 1S and FlushingPipeline
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false); //1N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true) * 2; //2S
    }

    private void Bx(uint instruction)
    {
        var rn = (int)instruction & 0xF;
        var target = Registers[rn];

        Registers.Cpsr.ThumbState = (target & 1) != 0;

        target &= ~1u; //clear bit 0 because to realign memory
        Registers.ProgramCounter = target;

        //2S + 1N cycles basically 1S and FlushingPipeline
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false); //1N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true); //2S
    }

    private void Swi(uint instruction)
    {
        var comment = instruction & 0x00FFFFFFu;
        Console.WriteLine("ARM SWI: comment = " + comment.ToString("X8"));

        Registers.SetSpsr(CpuMode.Supervisor, Registers.Cpsr);

        var newCpsr = Registers.Cpsr.ToUInt32();
        newCpsr = (newCpsr & ~0x1Fu) | (uint)CpuMode.Supervisor; //set supervisor mode
        newCpsr = BitUtils.SetBit(newCpsr, 7, true); //set irq disable
        newCpsr = BitUtils.SetBit(newCpsr, 5, false); //disable thumb

        //TODO: mode
        Registers.Cpsr = ProgramStatusRegister.FromUInt32(newCpsr);
        Registers[14] = Registers.ProgramCounter;
        Registers.ProgramCounter = 0x8; //vector address 0x8

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false); //N
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true) * 2; //2S cycles

        var functionVector = comment >> 16;
    }

    private void Illegal(uint instruction)
    {
        throw new InvalidOperationException("Illegal instruction");
    }
}