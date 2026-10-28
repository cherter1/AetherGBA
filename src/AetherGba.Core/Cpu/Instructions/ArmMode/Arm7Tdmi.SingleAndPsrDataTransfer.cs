using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu;

public sealed partial class Arm7Tdmi
{
    /* SINGLE DATA AND PSR TRANSFER INSTRUCTION ENCODINGS

      |..3 ..................2 ..................1 ..................0|
      |1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0_9_8_7_6_5_4_3_2_1_0|
      |_Cond__|0_1_0|P|U|B|W|L|__Rn___|__Rd___|_________Offset________| TransImm9
      |_Cond__|0_1_1|P|U|B|W|L|__Rn___|__Rd___|__Shift__|Typ|0|__Rm___| TransReg9
      |_Cond__|0_0_0_1_0|P|0_0_1_1_1_1|__Rd___|0_0_0_0_0_0_0_0_0_0_0_0| MRS reg
      |_Cond__|0_0_0_1_0|P|1_0|F|0_0|C|1_1_1_1|0_0_0_0_0_0_0_0|__Rm___| MSR reg
      |_Cond__|0_0|I|1_0|P|1_0|F|0_0|C|1_1_1_1|_Shift_|___Immediate___| MSR imm
     */

    private void Str(uint instruction)
    {
        var isOffsetImmediate = (instruction & 0x02000000) == 0;
        var preIndex = (instruction & 0x1000000) != 0; //bit 24
        var addOffset = (instruction & 0x800000) != 0; //bit 23
        var byteTransfer = (instruction & 0x400000) != 0; //bit 22
        var writeback = (instruction & 0x200000) != 0; //bit 21

        var baseRegister = (int)((instruction >> 16) & 0xF);
        var destinationRegister = (int)(instruction >> 12) & 0xF;
        var offset = isOffsetImmediate
            ? instruction & 0xFFF
            : ComputeShiftedRegisterOperand(instruction, out _);

        var address = baseRegister == 15
            ? Registers[baseRegister] + 4
            : Registers[baseRegister];
        var effectiveAddress = preIndex
            ? addOffset ? address + offset : address - offset
            : address;

        var writeValue = destinationRegister == 15
            ? Registers[destinationRegister] + 8
            : Registers[destinationRegister];

        if (byteTransfer)
        {
            _bus.Write8(effectiveAddress, (byte)writeValue); //STRB
            _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Byte, sequential: false); //N cycle
        }
        else
        {
            _bus.Write32(effectiveAddress, writeValue); //STR
            _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N cycle
        }

        if (!preIndex)
        {
            Registers[baseRegister] = addOffset ? address + offset : address - offset;
        }
        else if (writeback)
        {
            Registers[baseRegister] = effectiveAddress;
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false); //STR N
    }

    private void Ldr(uint instruction)
    {
        var isOffsetImmediate = (instruction & 0x02000000) == 0;
        var preIndex = (instruction & 0x1000000) != 0; //bit 24
        var addOffset = (instruction & 0x800000) != 0; //bit 23
        var byteTransfer = (instruction & 0x400000) != 0; //bit 22
        var writeback = (instruction & 0x200000) != 0; //bit 21

        var baseRegister = (int)((instruction >> 16) & 0xF);
        var destinationRegister = (int)(instruction >> 12) & 0xF;
        var offset = isOffsetImmediate
            ? instruction & 0xFFF
            : ComputeShiftedRegisterOperand(instruction, out _);

        var address = baseRegister == 15
            ? Registers[baseRegister] + 4
            : Registers[baseRegister];
        var effectiveAddress = preIndex
            ? addOffset ? address + offset : address - offset
            : address;

        uint loadedWord;

        if (byteTransfer)
        {
            loadedWord = _bus.Read8(effectiveAddress); //LDRB
            _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Byte, sequential: false); //N cycle
        }
        else
        {
            loadedWord = _bus.Read32(effectiveAddress); //LDR
            _cycles += _bus.GetCpuAccessCycles(effectiveAddress, AccessWidth.Word, sequential: false); //N cycle
        }

        if (!preIndex)
        {
            Registers[baseRegister] = addOffset
                ? address + offset
                : address - offset;
        }
        else if (writeback)
        {
            Registers[baseRegister] = effectiveAddress;
        }

        Registers[destinationRegister] = loadedWord;

        _cycles++; //1I
        if (destinationRegister == 15)
        {
            //if LDR PC add another 1S and 1N for pipeline refill
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: false);
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true) * 2;
            return;
        }

        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true); //LDR S
    }

    private void Mrs(uint instruction)
    {
        var useSpsr = (instruction & 0x00400000) != 0; //bit 22
        var rd = (int)(instruction >> 12) & 0xF;
        var statusReg = useSpsr ? Registers.GetSpsr().ToUInt32() : Registers.Cpsr.ToUInt32();

        Registers[rd] = statusReg;

        //1S cycle
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void Msr(uint instruction)
    {
        var useSpsr = (instruction & 0x00400000) != 0; //bit 22
        var flagBit = (instruction & 0x00080000) != 0; //bit 19
        var controlBit = (instruction & 0x10000) != 0; //bit 16
        var rm = (int)instruction & 0xf;

        var source = Registers[rm];
        var oldPsr = useSpsr ? Registers.GetSpsr().ToUInt32() : Registers.Cpsr.ToUInt32();

        uint newPsr = flagBit switch
        {
            true when controlBit => source,
            true => (oldPsr & 0x0FFFFFFFu) | (source & 0xF0000000u),
            false when controlBit => (oldPsr & 0xFFFFFF00u) | (source & 0xFFu),
            _ => oldPsr
        };

        var status = ProgramStatusRegister.FromUInt32(newPsr);
        if (useSpsr)
        {
            Registers.SetSpsr(Registers.Cpsr.Mode, status);
        }
        else
        {
            //TODO: Mode
            Registers.Cpsr = status;
        }

        //1S cycle
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }

    private void MsrImm(uint instruction)
    {
        var useSpsr = (instruction & 0x00400000) != 0; //bit 22
        var flagBit = (instruction & 0x00080000) != 0; //bit 19
        var controlBit = (instruction & 0x10000) != 0; //bit 16

        var source = DecodeImmediateOperand(instruction, out _);
        var oldPsr = useSpsr ? Registers.GetSpsr().ToUInt32() : Registers.Cpsr.ToUInt32();

        uint newPsr = flagBit switch
        {
            true when controlBit => source,
            true => (oldPsr & 0x0FFFFFFFu) | (source & 0xF0000000u),
            false when controlBit => (oldPsr & 0xFFFFFF00u) | (source & 0xFFu),
            _ => oldPsr
        };

        var status = ProgramStatusRegister.FromUInt32(newPsr);
        if (useSpsr)
        {
            Registers.SetSpsr(Registers.Cpsr.Mode, status);
        }
        else
        {
            //TODO: Mode
            Registers.Cpsr = status;
        }

        //1S cycle
        _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
    }
}