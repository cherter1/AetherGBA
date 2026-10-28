using System.Diagnostics;
using System.Runtime.CompilerServices;
using AetherGba.Core.Common;
using AetherGba.Core.Interrupts;
using AetherGba.Core.Memory;

namespace AetherGba.Core.Cpu;

public sealed partial class Arm7Tdmi
{
    private int _cycles;
    private readonly GbaBus _bus;
    private readonly InterruptController _interrupts;

    public delegate void ExecuteArmInstruction(uint instruction);
    public readonly ExecuteArmInstruction[] ArmInstructionDispatch;

    public delegate void ExecuteThumbInstruction(ushort instruction);
    public readonly ExecuteThumbInstruction[] ThumbInstructionDispatch;

    public RegisterBank Registers { get; private set; } = null!;

    public Arm7Tdmi(GbaBus bus, InterruptController interrupts)
    {
        _bus = bus;
        _interrupts = interrupts;
        ArmInstructionDispatch = GenerateArmInstructionTable();
        ThumbInstructionDispatch = GenerateThumbInstructionTable();
    }

    public void Reset()
    {
        Registers = new RegisterBank();
        Registers.InitializeForGba();

        Registers.Cpsr = new ProgramStatusRegister
        {
            Mode = CpuMode.System,
            IrqDisable = false,
            ThumbState = false
        };

        Registers.ProgramCounter = 0u;
    }

    public int Step()
    {
        if (!Registers.Cpsr.IrqDisable && _interrupts.ServiceIrq)
        {
            EnterIrqException();
            return 4;
        }

        _cycles = 0;
        if (Registers.Cpsr.ThumbState)
        {
            StepThumb();
        }
        else
        {
            StepArm();
        }

        return _cycles;
    }

    private void StepArm()
    {
        var instructionAddress = Registers.ProgramCounter;

        var instruction = _bus.Read32(instructionAddress);
        Registers.ProgramCounter = instructionAddress + 4;

        if (!ConditionPassed((Condition)(instruction >> 28))) //bits 31-28
        {
            _cycles += _bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
            return;
        }

        uint decodeBits = ((instruction >> 16) & 0xFF0) | ((instruction >> 4) & 0xF);
        ArmInstructionDispatch[decodeBits](instruction);
    }

    private void StepThumb()
    {
        var instructionAddress = Registers.ProgramCounter;
        var instruction = _bus.Read16(instructionAddress);
        Registers.ProgramCounter = instructionAddress + 2;

        var decodeBits = instruction >> 6;
        ThumbInstructionDispatch[decodeBits](instruction);
    }

    private uint DecodeImmediateOperand(uint instruction, out bool carryOut)
    {
        var immediate = instruction & 0xFF;
        var rotate = (int)((instruction >> 8) & 0xF) * 2;
        var result = BitUtils.RotateRight(immediate, rotate);
        carryOut = rotate == 0 ? Registers.Cpsr.Carry : BitUtils.IsBitSet(result, 31);
        return result;
    }

    private uint ComputeShiftedRegisterOperand(uint instruction, out bool carryOut)
    {
        var rm = (int)(instruction & 0xF);
        var rs = (int)(instruction >> 8) & 0xF;
        var registerShift = BitUtils.IsBitSet(instruction, 4);
        var shiftAmount = registerShift
            ? (int)(Registers[rs] & 0xFF)
            : (int)((instruction >> 7) & 0x1F);

        var shiftType = (instruction >> 5) & 0x3;
        var value = rm == 15
            ? registerShift
                ? Registers.ProgramCounter + 8 // rn and/or rm = instAddr + 12 if shifted register operand
                : Registers.ProgramCounter + 4 //otherwise instAddr + 8
            : Registers[rm];

        return shiftType switch
        {
            0 => ShiftLeft(value, shiftAmount, out carryOut),
            1 => ShiftRightLogical(value, shiftAmount, registerShift, out carryOut),
            2 => ShiftRightArithmetic(value, shiftAmount, registerShift, out carryOut),
            3 => RotateRight(value, shiftAmount, registerShift, out carryOut),
            _ => throw new UnreachableException()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ConditionPassed(Condition condition) =>
        condition switch
        {
            Condition.Eq => Registers.Cpsr.Zero,
            Condition.Ne => !Registers.Cpsr.Zero,
            Condition.Cs => Registers.Cpsr.Carry,
            Condition.Cc => !Registers.Cpsr.Carry,
            Condition.Mi => Registers.Cpsr.Negative,
            Condition.Pl => !Registers.Cpsr.Negative,
            Condition.Vs => Registers.Cpsr.Overflow,
            Condition.Vc => !Registers.Cpsr.Overflow,
            Condition.Hi => Registers.Cpsr is { Carry: true, Zero: false },
            Condition.Ls => !Registers.Cpsr.Carry || Registers.Cpsr.Zero,
            Condition.Ge => Registers.Cpsr.Negative == Registers.Cpsr.Overflow,
            Condition.Lt => Registers.Cpsr.Negative != Registers.Cpsr.Overflow,
            Condition.Gt => !Registers.Cpsr.Zero && Registers.Cpsr.Negative == Registers.Cpsr.Overflow,
            Condition.Le => Registers.Cpsr.Zero || Registers.Cpsr.Negative != Registers.Cpsr.Overflow,
            Condition.Al => true, _ => false
        };

    private void EnterIrqException()
    {
        var nextInstructionAddress = Registers.ProgramCounter;
        Registers.ProgramCounter = 0x18;
        Registers.SetSpsr(CpuMode.Irq, Registers.Cpsr);
        Registers.Cpsr = new ProgramStatusRegister
        {
            Mode = CpuMode.Irq,
            IrqDisable = true,
            ThumbState = false,
            Negative = Registers.Cpsr.Negative,
            Zero = Registers.Cpsr.Zero,
            Carry = Registers.Cpsr.Carry,
            Overflow = Registers.Cpsr.Overflow
        };
        //TODO reg mode
        Registers[14] = nextInstructionAddress + 4u;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateNz(uint result)
    {
        Registers.Cpsr.Negative = (result & 0x80000000) != 0;
        Registers.Cpsr.Zero = result == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCarry(bool carry) =>
        Registers.Cpsr.Carry = carry;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetOverflow(bool overflow) =>
        Registers.Cpsr.Overflow = overflow;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNegative(bool negative) =>
        Registers.Cpsr.Negative = negative;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetZero(bool zero) =>
        Registers.Cpsr.Zero = zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateArithmeticFlags(uint left, uint right, uint result, bool subtraction)
    {
        Registers.Cpsr.Negative = (result & 0x80000000) != 0;
        Registers.Cpsr.Zero = result == 0;

        if (subtraction)
        {
            Registers.Cpsr.Carry = left >= right;
            Registers.Cpsr.Overflow = ((left ^ right) & (left ^ result) & 0x80000000) != 0;
        }
        else
        {
            Registers.Cpsr.Carry = result < left || result < right;
            Registers.Cpsr.Overflow = (~(left ^ right) & (left ^ result) & 0x80000000) != 0;
        }
    }

    private uint ShiftLeft(uint value, int amount, out bool carryOut)
    {
        switch (amount)
        {
            case 0:
                carryOut = Registers.Cpsr.Carry;
                return value;
            case >= 32:
                //last bit shifted out if == 32 otherwise always no carry out
                carryOut = amount == 32 && BitUtils.IsBitSet(value, 0);
                return 0;
            default:
                carryOut = ((value >> (32 - amount)) & 1U) != 0;
                return value << amount;
        }
    }

    private uint ShiftRightLogical(uint value, int amount, bool registerShift, out bool carryOut)
    {
        switch (amount)
        {
            case 0 when registerShift:
                carryOut = Registers.Cpsr.Carry;
                return value;
            case 0:
                carryOut = BitUtils.IsBitSet(value, 31);
                return 0;
            case >= 32:
                carryOut = amount == 32 && BitUtils.IsBitSet(value, 31);
                return 0;
            default:
                carryOut = ((value >> (amount - 1)) & 1U) != 0;
                return value >> amount;
        }
    }

    private uint ShiftRightArithmetic(uint value, int amount, bool registerShift, out bool carryOut)
    {
        switch (amount)
        {
            case 0 when !registerShift:
            case >= 32:
                carryOut = BitUtils.IsBitSet(value, 31);
                return carryOut ? 0xFFFFFFFF : 0;
            case 0:
                carryOut = Registers.Cpsr.Carry;
                return value;
            default:
                carryOut = ((value >> (amount - 1)) & 1U) != 0;
                return (uint)((int)value >> amount);
        }
    }

    private uint RotateRight(uint value, int amount, bool registerShift, out bool carryOut)
    {
        if (amount == 0 && !registerShift) //ror#0 is interpreted as rrx#1, like ror#1 but result bit 31 is set to old C
        {
            carryOut = BitUtils.IsBitSet(value, 0);
            var rotated = BitUtils.RotateRight(value, 1);
            rotated = BitUtils.SetBit(rotated, 31, Registers.Cpsr.Carry);
            return rotated;
        }

        var result = BitUtils.RotateRight(value, amount);
        carryOut = amount == 0 ?
            Registers.Cpsr.Carry : //when rs == 0 carry remains unchanged
            BitUtils.IsBitSet(result, 31);
        return result;
    }

    private static int GetMultiplierArrayCycles(uint multiplierOperand, bool unSigned)
    {
        const uint SingleCycleMask = 0xffffff00;
        const uint DoubleCycleMask = 0xffff0000;
        const uint TripleCycleMask = 0xff000000;
        if (unSigned)
        {
            if ((multiplierOperand & SingleCycleMask) == 0)
            {
                return 1;
            }

            if ((multiplierOperand & DoubleCycleMask) == 0)
            {
                return 2;
            }

            return (multiplierOperand & TripleCycleMask) == 0 ? 3 : 4;
        }

        if ((multiplierOperand & SingleCycleMask) is 0 or SingleCycleMask)
        {
            return 1;
        }

        if ((multiplierOperand & DoubleCycleMask) is 0 or DoubleCycleMask)
        {
            return 2;
        }

        return (multiplierOperand & TripleCycleMask) is 0 or TripleCycleMask ? 3 : 4;
    }

    public ExecuteArmInstruction[] GenerateArmInstructionTable()
    {
        ExecuteArmInstruction[] table = new ExecuteArmInstruction[4096];
        for (uint i = 0; i < 4096; i++)
        {
            uint opCode = ((i & 0xFF0) << 16) | ((i & 0xF) << 4);
            table[i] = DecodeArmInstruction(opCode);
        }

        return table;
    }

    private ExecuteArmInstruction DecodeArmInstruction(uint instruction)
    {
        var bits27_25 = (instruction >> 25) & 0b111;
        if (bits27_25 == 0b111)
        {
            return Swi;
            //return "SWI";
        }
        else if (bits27_25 == 0b101)
        {
            if ((instruction & 0x01000000) == 0) //bit 24
            {
                return B;
                //return "B";
            }
            else
            {
                return Bl;
                //return "BL";
            }
        }
        else if (bits27_25 == 0b100)
        {
            if ((instruction & 0x00100000) == 0)
            {
                return Stm;
                //return "STM";
            }
            else
            {
                return Ldm;
                //return "LDM";
            }
        }
        else if (bits27_25 == 0b011)
        {
            if ((instruction & 0x00100000) == 0) //bit 20
            {
                if ((instruction & 0x00400000) == 0) //bit 22
                {
                    return Str;
                    //return "STR";
                }

                return Str;
                //return "STRB";
            }
            else
            {
                if ((instruction & 0x00400000) == 0) //bit 22
                {
                    return Ldr;
                    //return "LDR";
                }

                return Ldr;
                //return "LDRB";
            }
        }
        else if (bits27_25 == 0b010)
        {
            if ((instruction & 0x00100000) == 0) //bit 20
            {
                if ((instruction & 0x00400000) == 0) //bit 22
                {
                    return Str;
                    //return "STR Imm";
                }

                return Str;
                //return "STRB Imm";
            }
            else
            {
                if ((instruction & 0x00400000) == 0) //bit 22
                {
                    return Ldr;
                    //return "LDR Imm";
                }

                return Ldr;
                //return "LDRB Imm";
            }
        }
        else if (bits27_25 == 0b001)
        {
            var bits24_20 = (instruction >> 20) & 0x1f;

            if ((bits24_20 & 0b11011) == 0b10010)
            {
                return MsrImm;
                //return "MSR Imm";
            }
            else if (bits24_20 == 0b10001)
            {
                return TstImm;
                //return "TST Imm";
            }
            else if (bits24_20 == 0b10011)
            {
                return TeqImm;
                //return "TEQ Imm";
            }
            else if (bits24_20 == 0b10101)
            {
                return CmpImm;
                //return "CMP Imm";
            }
            else if (bits24_20 == 0b10111)
            {
                return CmnImm;
                //return "CMN Imm";
            }
            else if ((bits24_20 >> 1) == 0)
            {
                return AndImm;
                //return "AND Imm";
            }
            else if ((bits24_20 >> 1) == 0b0001)
            {
                return EorImm;
                //return "EOR Imm";
            }
            else if ((bits24_20 >> 1) == 0b0010)
            {
                return SubImm;
                //return "SUB Imm";
            }
            else if ((bits24_20 >> 1) == 0b0011)
            {
                return RsbImm;
                //return "RSB Imm";
            }
            else if ((bits24_20 >> 1) == 0b0100)
            {
                return AddImm;
                //return "ADD Imm";
            }
            else if ((bits24_20 >> 1) == 0b0101)
            {
                return AdcImm;
                //return "ADC Imm";
            }
            else if ((bits24_20 >> 1) == 0b0110)
            {
                //return "SBC Imm";
                return SbcImm;
            }
            else if ((bits24_20 >> 1) == 0b0111)
            {
                //return "RSC Imm";
                return RscImm;
            }
            else if ((bits24_20 >> 1) == 0b1100)
            {
                return OrrImm;
                //return "ORR Imm";
            }
            else if ((bits24_20 >> 1) == 0b1101)
            {
                //return "MOV Imm";
                return MovImm;
            }
            else if ((bits24_20 >> 1) == 0b1110)
            {
                //return "BIC Imm";
                return BicImm;
            }
            else if ((bits24_20 >> 1) == 0b1111)
            {
                //return "MVN Imm";
                return MvnImm;
            }
        }
        else
        {
            var bits24_20 = (instruction >> 20) & 0x1f;
            var bits7_4 = (instruction >> 4) & 0xf;

            if (bits24_20 == 0b10010 && bits7_4 == 0x1)
            {
                //return "BX";
                return Bx;
            }
            else if (bits24_20 == 0b10000 && bits7_4 == 0b1001)
            {
                //return "SWP";
                return Swp;
            }
            else if (bits24_20 == 0b10100 && bits7_4 == 0b1001)
            {
                //return "SWPB";
                return Swpb;
            }
            else if ((bits24_20 & 0b11011) == 0b10010 && bits7_4 == 0)
            {
                //return "MSR";
                return Msr;
            }
            else if ((bits24_20 & 0b11011) == 0b10000 && bits7_4 == 0)
            {
                //return "MRS";
                return Mrs;
            }
            else if ((bits24_20 >> 1) == 0 && bits7_4 == 0b1001)
            {
                //return "MUL";
                return Mul;
            }
            else if ((bits24_20 >> 1) == 1 && bits7_4 == 0b1001)
            {
                //return "MLA";
                return Mla;
            }
            else if ((bits24_20 >> 1) == 0b100 && bits7_4 == 0b1001)
            {
                //return "UMULL";
                return Umull;
            }
            else if ((bits24_20 >> 1) == 0b101 && bits7_4 == 0b1001)
            {
                //return "UMLAL";
                return Umlal;
            }
            else if ((bits24_20 >> 1) == 0b110 && bits7_4 == 0b1001)
            {
                //return "SMULL";
                return Smull;
            }
            else if ((bits24_20 >> 1) == 0b111 && bits7_4 == 0b1001)
            {
                //return "SMLAL";
                return Smlal;
            }
            else if ((bits24_20 & 1) == 1 && bits7_4 == 0b1011)
            {
                //return "LDRH";
                return Ldrh;
            }
            else if ((bits24_20 & 1) == 1 && bits7_4 == 0b1101)
            {
                //return "LDRSB";
                return Ldrsb;
            }
            else if ((bits24_20 & 1) == 1 && bits7_4 == 0b1111)
            {
                //return "LDRSH";
                return Ldrsh;
            }
            else if ((bits24_20 & 1) == 0 && bits7_4 == 0b1011)
            {
                //return "STRH";
                return Strh;
            }
            else if (bits24_20 == 0b10001)
            {
                //return "TST";
                return Tst;
            }
            else if (bits24_20 == 0b10011)
            {
                //return "TEQ";
                return Teq;
            }
            else if (bits24_20 == 0b10101)
            {
                //return "CMP";
                return Cmp;
            }
            else if (bits24_20 == 0b10111)
            {
                //return "CMN";
                return Cmn;
            }
            else if ((bits24_20 >> 1) == 0)
            {
                //return "AND";
                return And;
            }
            else if ((bits24_20 >> 1) == 0b0001)
            {
                //return "EOR";
                return Eor;
            }
            else if ((bits24_20 >> 1) == 0b0010)
            {
                //return "SUB";
                return Sub;
            }
            else if ((bits24_20 >> 1) == 0b0011)
            {
                //return "RSB";
                return Rsb;
            }
            else if ((bits24_20 >> 1) == 0b0100)
            {
                //return "ADD";
                return Add;
            }
            else if ((bits24_20 >> 1) == 0b0101)
            {
                //return "ADC";
                return Adc;
            }
            else if ((bits24_20 >> 1) == 0b0110)
            {
                //return "SBC";
                return Sbc;
            }
            else if ((bits24_20 >> 1) == 0b0111)
            {
                //return "RSC";
                return Rsc;
            }
            else if ((bits24_20 >> 1) == 0b1100)
            {
                //return "ORR";
                return Orr;
            }
            else if ((bits24_20 >> 1) == 0b1101)
            {
                //return "MOV";
                return Mov;
            }
            else if ((bits24_20 >> 1) == 0b1110)
            {
                //return "BIC";
                return Bic;
            }
            else if ((bits24_20 >> 1) == 0b1111)
            {
                //return "MVN";
                return Mvn;
            }
        }

        return Illegal;
        //return "ILL";
    }

    public ExecuteThumbInstruction[] GenerateThumbInstructionTable()
    {
        ExecuteThumbInstruction[] table = new ExecuteThumbInstruction[1024];
        for (ushort i = 0; i < 1024; i++)
        {
            ushort opCode = (ushort)(i << 6);
            table[i] = DecodeThumbInstruction(opCode);
        }

        return table;
    }

    public ExecuteThumbInstruction DecodeThumbInstruction(ushort instruction)
    {
        var bits11_9 = (instruction >> 9) & 0b111;
        var bits11_6 = (instruction >> 6) & 0x3f;
        var bits12_11 = (instruction >> 11) & 0b11;
        var topBits = (instruction >> 12) & 0xf; //bits 15-12
        switch (topBits)
        {
            case 0b0000:
            case 0b0001:
                switch (bits12_11)
                {
                    case 0b00:
                        return LslImm; //f1
                    case 0b01:
                        return LsrImm; //f1
                    case 0b10:
                        return AsrImm; //f1
                    case 0b11:
                        if ((instruction & 0x200) != 0) //bit 9
                        {
                            return Sub; //f2
                        }
                        return Add; //f2
                }
                break; //f1 f2
            case 0b0010:
            case 0b0011:
                //f3
                return bits12_11 switch
                {
                    0b00 => MovImm,
                    0b01 => CmpImm,
                    0b10 => AddImm,
                    0b11 => SubImm,
                    _ => Illegal
                };
            case 0b0100:
                switch (bits11_6)
                {
                    case 0b000000:
                        return And; //f4
                    case 0b000001:
                        return Eor; //f4
                    case 0b000010:
                        return Lsl; //f4
                    case 0b000011:
                        return Lsr; //f4
                    case 0b000100:
                        return Asr; //f4
                    case 0b000101:
                        return Adc; //f4
                    case 0b000110:
                        return Sbc; //f4
                    case 0b000111:
                        return Ror; //f4
                    case 0b001000:
                        return Tst; //f4
                    case 0b001001:
                        return Neg; //f4
                    case 0b001010:
                        return Cmp; //f4
                    case 0b001011:
                        return Cmn; //f4
                    case 0b001100:
                        return Orr; //f4
                    case 0b001101:
                        return Mul; //f4
                    case 0b001110:
                        return Bic; //f4
                    case 0b001111:
                        return Mvn; //f4
                    default:
                        if ((instruction & 0x800) != 0) //bit 11
                        {
                            return LdrPc; //f6
                        }

                        return ((instruction >> 8) & 0xf) switch
                        {
                            0b0100 => AddHiReg, //f5
                            0b0101 => CmpHiReg, //f5
                            0b0110 => MovHiReg, //f5
                            0b0111 => Bx, //f5
                            _ => Illegal
                        };
                }
            case 0b0101:
                return bits11_9 switch
                {
                    0b000 => Str, //f7
                    0b001 => Strh, //f8
                    0b010 => Strb, //f7
                    0b011 => Ldsb, //f8
                    0b100 => Ldr, //f7
                    0b101 => Ldrh, //f8
                    0b110 => Ldrb, //f7
                    0b111 => Ldsh, //f8
                    _ => Illegal
                };
            case 0b0110:
            case 0b0111:
                //f9
                return bits12_11 switch
                {
                    0b00 => StrImm,
                    0b01 => LdrImm,
                    0b10 => StrbImm,
                    0b11 => LdrbImm,
                    _ => Illegal
                };
            case 0b1000:
                //f10
                if ((instruction & 0x800) != 0) //bit 11
                {
                    return LdrhImm;
                }
                return StrhImm;
            case 0b1001:
                //f11
                if ((instruction & 0x800) != 0) //bit 11
                {
                    return LdrWithSp;
                }
                return StrWithSp;
            case 0b1010:
                return AddWithPcOrSp; //f12
            case 0b1011:
                return bits11_9 switch
                {
                    0b000 => AddSubOffsetSp, //f13 sp (+/-)= imm
                    0b010 => Push, //f14
                    0b110 => Pop, //f14
                    _ => Illegal
                };
            case 0b1100:
                //f15
                if ((instruction & 0x800) != 0) //bit 11
                {
                    return Ldm;
                }
                return Stm;
            case 0b1101:
                if ((instruction & 0xf00) == 0xf00) //bits 11-8 == 0xf swi
                {
                    return Swi; //f17
                }
                return BCond; //f16
            case 0b1110:
                return B; //f18
            case 0b1111:
                return Bl; //f19
        }
        return Illegal;
    }
}