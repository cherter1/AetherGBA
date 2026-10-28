using AetherGba.Core.Common;

namespace AetherGba.Core.Cpu;

public struct ProgramStatusRegister
{
    // MSB of result
    public bool Negative { get; set; }

    // result of op is 0
    public bool Zero { get; set; }

    // shifts - last bit shifted out. 0b00101 << 3  = 0b01000 bit 2 on the left operand is 1 so C gets set HIGH
    // addition - carryout bit of addition i.e. 0b11 + 0b01 = 0b100 bit 2 is carryout so C gets set HIGH
    // subtraction - (not-borrow) set high if no borrow happened i.e. 3(0x00000003) - 5(0x00000005) = 0xFFFFFFFE wrapped around for borrow so C is set LOW (left >= right)
    public bool Carry { get; set; }

    // for signed overflow occurring set high
    // addition (positive + positive = negative) or (negative + negative = positive) set HIGH
    // subtraction - (positive - negative = negative) or (negative - positive = positive) set HIGH
    public bool Overflow { get; set; }

    public bool IrqDisable { get; set; }

    public bool FiqDisable { get; set; }

    public bool ThumbState { get; set; }

    public CpuMode Mode { get; set; }

    public uint ToUInt32()
    {
        var value = (uint)Mode;
        value = BitUtils.SetBit(value, 5, ThumbState);
        value = BitUtils.SetBit(value, 6, FiqDisable);
        value = BitUtils.SetBit(value, 7, IrqDisable);
        value = BitUtils.SetBit(value, 28, Overflow);
        value = BitUtils.SetBit(value, 29, Carry);
        value = BitUtils.SetBit(value, 30, Zero);
        value = BitUtils.SetBit(value, 31, Negative);
        return value;
    }

    public static ProgramStatusRegister FromUInt32(uint value) =>
        new()
        {
            Mode = (CpuMode)(value & 0x1F),
            ThumbState = BitUtils.IsBitSet(value, 5),
            FiqDisable = BitUtils.IsBitSet(value, 6),
            IrqDisable = BitUtils.IsBitSet(value, 7),
            Overflow = BitUtils.IsBitSet(value, 28),
            Carry = BitUtils.IsBitSet(value, 29),
            Zero = BitUtils.IsBitSet(value, 30),
            Negative = BitUtils.IsBitSet(value, 31)
        };
}