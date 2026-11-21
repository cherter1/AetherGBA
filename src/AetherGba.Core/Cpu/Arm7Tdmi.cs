using System.Diagnostics;
using System.Runtime.CompilerServices;
using AetherGba.Core.Common;
using AetherGba.Core.Interrupts;
using AetherGba.Core.Memory;
using AetherGba.Core.Cpu.Instructions.ThumbMode;
using AetherGba.Core.Cpu.Instructions.ArmMode;

namespace AetherGba.Core.Cpu;

public sealed partial class Arm7Tdmi
{
    public RegisterBank Registers { get; private set; } = null!;

    internal int Cycles;
    internal readonly GbaBus Bus;

    private readonly InterruptController _interrupts;

    private readonly unsafe delegate* managed<Arm7Tdmi, uint, void>[] _armInstructionDispatch;
    private readonly unsafe delegate* managed<Arm7Tdmi, ushort, void>[] _thumbInstructionDispatch;

    public unsafe Arm7Tdmi(GbaBus bus, InterruptController interrupts)
    {
        Bus = bus;
        _interrupts = interrupts;
        _armInstructionDispatch = GenerateArmInstructionTable();
        _thumbInstructionDispatch = GenerateThumbInstructionTable();
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

        Cycles = 0;
        if (Registers.Cpsr.ThumbState)
        {
            StepThumb();
        }
        else
        {
            StepArm();
        }

        return Cycles;
    }

    private unsafe void StepArm()
    {
        var instructionAddress = Registers.ProgramCounter;

        var instruction = Bus.Read32(instructionAddress);
        Registers.ProgramCounter = instructionAddress + 4;

        if (!ConditionPassed((Condition)(instruction >> 28))) //bits 31-28
        {
            Cycles += Bus.GetCpuAccessCycles(Registers.ProgramCounter, AccessWidth.Word, sequential: true);
            return;
        }

        uint decodeBits = ((instruction >> 16) & 0xFF0) | ((instruction >> 4) & 0xF);
        _armInstructionDispatch[decodeBits](this, instruction);
    }

    private unsafe void StepThumb()
    {
        var instructionAddress = Registers.ProgramCounter;
        var instruction = Bus.ReadThumbInstruction(instructionAddress);
        //var instruction = Bus.Read16(instructionAddress);
        Registers.ProgramCounter = instructionAddress + 2;

        var decodeBits = instruction >> 6;
        _thumbInstructionDispatch[decodeBits](this, instruction);
    }

    internal uint DecodeImmediateOperand(uint instruction, out bool carryOut)
    {
        var immediate = instruction & 0xFF;
        var rotate = (int)((instruction >> 8) & 0xF) * 2;
        var result = BitUtils.RotateRight(immediate, rotate);
        carryOut = rotate == 0 ? Registers.Cpsr.Carry : BitUtils.IsBitSet(result, 31);
        return result;
    }

    internal uint ComputeShiftedRegisterOperand(uint instruction, out bool carryOut)
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
    internal bool ConditionPassed(Condition condition) =>
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
    internal void UpdateNz(uint result)
    {
        Registers.Cpsr.Negative = (result & 0x80000000) != 0;
        Registers.Cpsr.Zero = result == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetCarry(bool carry) =>
        Registers.Cpsr.Carry = carry;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetOverflow(bool overflow) =>
        Registers.Cpsr.Overflow = overflow;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetNegative(bool negative) =>
        Registers.Cpsr.Negative = negative;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetZero(bool zero) =>
        Registers.Cpsr.Zero = zero;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UpdateArithmeticFlags(uint left, uint right, uint result, bool subtraction)
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

    internal uint ShiftLeft(uint value, int amount, out bool carryOut)
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

    internal uint ShiftRightLogical(uint value, int amount, bool registerShift, out bool carryOut)
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

    internal uint ShiftRightArithmetic(uint value, int amount, bool registerShift, out bool carryOut)
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

    internal uint RotateRight(uint value, int amount, bool registerShift, out bool carryOut)
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

    internal static int GetMultiplierArrayCycles(uint multiplierOperand, bool unSigned)
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

    private static unsafe delegate* managed<Arm7Tdmi, uint, void>[] GenerateArmInstructionTable()
    {
        var table = new delegate* managed<Arm7Tdmi, uint, void>[4096];
        for (uint i = 0; i < 4096; i++)
        {
            uint opCode = ((i & 0xFF0) << 16) | ((i & 0xF) << 4);
            table[i] = DecodeArmInstruction(opCode);
        }

        return table;
    }

    private static unsafe delegate* managed<Arm7Tdmi, uint, void> DecodeArmInstruction(uint instruction)
    {
        var bits27_25 = (instruction >> 25) & 0b111;
        if (bits27_25 == 0b111)
        {
            return &ArmOps.Swi;
        }
        else if (bits27_25 == 0b101)
        {
            if ((instruction & 0x01000000) == 0) //bit 24
            {
                return &ArmOps.B;
            }
            else
            {
                return &ArmOps.Bl;
            }
        }
        else if (bits27_25 == 0b100)
        {
            if ((instruction & 0x00100000) == 0)
            {
                return &ArmOps.Stm;
            }
            else
            {
                return &ArmOps.Ldm;
            }
        }
        else if (bits27_25 == 0b011)
        {
            if ((instruction & 0x00100000) == 0) //bit 20
            {
                if ((instruction & 0x00400000) == 0) //bit 22
                {
                    return &ArmOps.Str;
                }

                return &ArmOps.Str;
            }
            else
            {
                if ((instruction & 0x00400000) == 0) //bit 22
                {
                    return &ArmOps.Ldr;
                }

                return &ArmOps.Ldr;
            }
        }
        else if (bits27_25 == 0b010)
        {
            if ((instruction & 0x00100000) == 0) //bit 20
            {
                if ((instruction & 0x00400000) == 0) //bit 22
                {
                    return &ArmOps.Str;
                }

                return &ArmOps.Str;
            }
            else
            {
                if ((instruction & 0x00400000) == 0) //bit 22
                {
                    return &ArmOps.Ldr;
                }

                return &ArmOps.Ldr;
            }
        }
        else if (bits27_25 == 0b001)
        {
            var bits24_20 = (instruction >> 20) & 0x1f;

            if ((bits24_20 & 0b11011) == 0b10010)
            {
                return &ArmOps.MsrImm;
            }
            else if (bits24_20 == 0b10001)
            {
                return &ArmOps.TstImm;
            }
            else if (bits24_20 == 0b10011)
            {
                return &ArmOps.TeqImm;
            }
            else if (bits24_20 == 0b10101)
            {
                return &ArmOps.CmpImm;
            }
            else if (bits24_20 == 0b10111)
            {
                return &ArmOps.CmnImm;
            }
            else if ((bits24_20 >> 1) == 0)
            {
                return &ArmOps.AndImm;
            }
            else if ((bits24_20 >> 1) == 0b0001)
            {
                return &ArmOps.EorImm;
            }
            else if ((bits24_20 >> 1) == 0b0010)
            {
                return &ArmOps.SubImm;
            }
            else if ((bits24_20 >> 1) == 0b0011)
            {
                return &ArmOps.RsbImm;
            }
            else if ((bits24_20 >> 1) == 0b0100)
            {
                return &ArmOps.AddImm;
            }
            else if ((bits24_20 >> 1) == 0b0101)
            {
                return &ArmOps.AdcImm;
            }
            else if ((bits24_20 >> 1) == 0b0110)
            {
                return &ArmOps.SbcImm;
            }
            else if ((bits24_20 >> 1) == 0b0111)
            {
                return &ArmOps.RscImm;
            }
            else if ((bits24_20 >> 1) == 0b1100)
            {
                return &ArmOps.OrrImm;
            }
            else if ((bits24_20 >> 1) == 0b1101)
            {
                return &ArmOps.MovImm;
            }
            else if ((bits24_20 >> 1) == 0b1110)
            {
                return &ArmOps.BicImm;
            }
            else if ((bits24_20 >> 1) == 0b1111)
            {
                return &ArmOps.MvnImm;
            }
        }
        else
        {
            var bits24_20 = (instruction >> 20) & 0x1f;
            var bits7_4 = (instruction >> 4) & 0xf;

            if (bits24_20 == 0b10010 && bits7_4 == 0x1)
            {
                return &ArmOps.Bx;
            }
            else if (bits24_20 == 0b10000 && bits7_4 == 0b1001)
            {
                return &ArmOps.Swp;
            }
            else if (bits24_20 == 0b10100 && bits7_4 == 0b1001)
            {
                return &ArmOps.Swpb;
            }
            else if ((bits24_20 & 0b11011) == 0b10010 && bits7_4 == 0)
            {
                return &ArmOps.Msr;
            }
            else if ((bits24_20 & 0b11011) == 0b10000 && bits7_4 == 0)
            {
                return &ArmOps.Mrs;
            }
            else if ((bits24_20 >> 1) == 0 && bits7_4 == 0b1001)
            {
                return &ArmOps.Mul;
            }
            else if ((bits24_20 >> 1) == 1 && bits7_4 == 0b1001)
            {
                return &ArmOps.Mla;
            }
            else if ((bits24_20 >> 1) == 0b100 && bits7_4 == 0b1001)
            {
                return &ArmOps.Umull;
            }
            else if ((bits24_20 >> 1) == 0b101 && bits7_4 == 0b1001)
            {
                return &ArmOps.Umlal;
            }
            else if ((bits24_20 >> 1) == 0b110 && bits7_4 == 0b1001)
            {
                return &ArmOps.Smull;
            }
            else if ((bits24_20 >> 1) == 0b111 && bits7_4 == 0b1001)
            {
                return &ArmOps.Smlal;
            }
            else if ((bits24_20 & 1) == 1 && bits7_4 == 0b1011)
            {
                return &ArmOps.Ldrh;
            }
            else if ((bits24_20 & 1) == 1 && bits7_4 == 0b1101)
            {
                return &ArmOps.Ldrsb;
            }
            else if ((bits24_20 & 1) == 1 && bits7_4 == 0b1111)
            {
                return &ArmOps.Ldrsh;
            }
            else if ((bits24_20 & 1) == 0 && bits7_4 == 0b1011)
            {
                return &ArmOps.Strh;
            }
            else if (bits24_20 == 0b10001)
            {
                return &ArmOps.Tst;
            }
            else if (bits24_20 == 0b10011)
            {
                return &ArmOps.Teq;
            }
            else if (bits24_20 == 0b10101)
            {
                return &ArmOps.Cmp;
            }
            else if (bits24_20 == 0b10111)
            {
                return &ArmOps.Cmn;
            }
            else if ((bits24_20 >> 1) == 0)
            {
                return &ArmOps.And;
            }
            else if ((bits24_20 >> 1) == 0b0001)
            {
                return &ArmOps.Eor;
            }
            else if ((bits24_20 >> 1) == 0b0010)
            {
                return &ArmOps.Sub;
            }
            else if ((bits24_20 >> 1) == 0b0011)
            {
                return &ArmOps.Rsb;
            }
            else if ((bits24_20 >> 1) == 0b0100)
            {
                return &ArmOps.Add;
            }
            else if ((bits24_20 >> 1) == 0b0101)
            {
                return &ArmOps.Adc;
            }
            else if ((bits24_20 >> 1) == 0b0110)
            {
                return &ArmOps.Sbc;
            }
            else if ((bits24_20 >> 1) == 0b0111)
            {
                return &ArmOps.Rsc;
            }
            else if ((bits24_20 >> 1) == 0b1100)
            {
                return &ArmOps.Orr;
            }
            else if ((bits24_20 >> 1) == 0b1101)
            {
                return &ArmOps.Mov;
            }
            else if ((bits24_20 >> 1) == 0b1110)
            {
                return &ArmOps.Bic;
            }
            else if ((bits24_20 >> 1) == 0b1111)
            {
                return &ArmOps.Mvn;
            }
        }

        return &ArmOps.Illegal;
    }

    private static unsafe delegate* managed<Arm7Tdmi, ushort, void>[] GenerateThumbInstructionTable()
    {
        var table = new delegate* managed<Arm7Tdmi, ushort, void>[1024];
        for (ushort i = 0; i < 1024; i++)
        {
            ushort opCode = (ushort)(i << 6);
            table[i] = DecodeThumbInstruction(opCode);
        }

        return table;
    }

    private static unsafe delegate* managed<Arm7Tdmi, ushort, void> DecodeThumbInstruction(ushort instruction)
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
                        return &ThumbOps.LslImm; //f1
                    case 0b01:
                        return &ThumbOps.LsrImm; //f1
                    case 0b10:
                        return &ThumbOps.AsrImm; //f1
                    case 0b11:
                        if ((instruction & 0x200) != 0) //bit 9
                        {
                            return &ThumbOps.Sub; //f2
                        }
                        return &ThumbOps.Add; //f2
                }
                break; //f1 f2
            case 0b0010:
            case 0b0011:
                //f3
                return bits12_11 switch
                {
                    0b00 => &ThumbOps.MovImm,
                    0b01 => &ThumbOps.CmpImm,
                    0b10 => &ThumbOps.AddImm,
                    0b11 => &ThumbOps.SubImm,
                    _ => &ThumbOps.Illegal
                };
            case 0b0100:
                switch (bits11_6)
                {
                    case 0b000000: return &ThumbOps.And; //f4
                    case 0b000001: return &ThumbOps.Eor; //f4
                    case 0b000010: return &ThumbOps.Lsl; //f4
                    case 0b000011: return &ThumbOps.Lsr; //f4
                    case 0b000100: return &ThumbOps.Asr; //f4
                    case 0b000101: return &ThumbOps.Adc; //f4
                    case 0b000110: return &ThumbOps.Sbc; //f4
                    case 0b000111: return &ThumbOps.Ror; //f4
                    case 0b001000: return &ThumbOps.Tst; //f4
                    case 0b001001: return &ThumbOps.Neg; //f4
                    case 0b001010: return &ThumbOps.Cmp; //f4
                    case 0b001011: return &ThumbOps.Cmn; //f4
                    case 0b001100: return &ThumbOps.Orr; //f4
                    case 0b001101: return &ThumbOps.Mul; //f4
                    case 0b001110: return &ThumbOps.Bic; //f4
                    case 0b001111: return &ThumbOps.Mvn; //f4
                    default:
                        if ((instruction & 0x800) != 0) //bit 11
                        {
                            return &ThumbOps.LdrPc; //f6
                        }

                        return ((instruction >> 8) & 0xf) switch
                        {
                            0b0100 => &ThumbOps.AddHiReg, //f5
                            0b0101 => &ThumbOps.CmpHiReg, //f5
                            0b0110 => &ThumbOps.MovHiReg, //f5
                            0b0111 => &ThumbOps.Bx, //f5
                            _ => &ThumbOps.Illegal
                        };
                }
            case 0b0101:
                return bits11_9 switch
                {
                    0b000 => &ThumbOps.Str, //f7
                    0b001 => &ThumbOps.Strh, //f8
                    0b010 => &ThumbOps.Strb, //f7
                    0b011 => &ThumbOps.Ldsb, //f8
                    0b100 => &ThumbOps.Ldr, //f7
                    0b101 => &ThumbOps.Ldrh, //f8
                    0b110 => &ThumbOps.Ldrb, //f7
                    0b111 => &ThumbOps.Ldsh, //f8
                    _ => &ThumbOps.Illegal
                };
            case 0b0110:
            case 0b0111:
                //f9
                return bits12_11 switch
                {
                    0b00 => &ThumbOps.StrImm,
                    0b01 => &ThumbOps.LdrImm,
                    0b10 => &ThumbOps.StrbImm,
                    0b11 => &ThumbOps.LdrbImm,
                    _ => &ThumbOps.Illegal
                };
            case 0b1000:
                //f10
                if ((instruction & 0x800) != 0) //bit 11
                {
                    return &ThumbOps.LdrhImm;
                }
                return &ThumbOps.StrhImm;
            case 0b1001:
                //f11
                if ((instruction & 0x800) != 0) //bit 11
                {
                    return &ThumbOps.LdrWithSp;
                }
                return &ThumbOps.StrWithSp;
            case 0b1010:
                return &ThumbOps.AddWithPcOrSp; //f12
            case 0b1011:
                return bits11_9 switch
                {
                    0b000 => &ThumbOps.AddSubOffsetSp, //f13 sp (+/-)= imm
                    0b010 => &ThumbOps.Push, //f14
                    0b110 => &ThumbOps.Pop, //f14
                    _ => &ThumbOps.Illegal
                };
            case 0b1100:
                //f15
                if ((instruction & 0x800) != 0) //bit 11
                {
                    return &ThumbOps.Ldm;
                }
                return &ThumbOps.Stm;
            case 0b1101:
                if ((instruction & 0xf00) == 0xf00) //bits 11-8 == 0xf swi
                {
                    return &ThumbOps.Swi; //f17
                }
                return &ThumbOps.BCond; //f16
            case 0b1110:
                return &ThumbOps.B; //f18
            case 0b1111:
                return &ThumbOps.Bl; //f19
        }
        return &ThumbOps.Illegal;
    }
}