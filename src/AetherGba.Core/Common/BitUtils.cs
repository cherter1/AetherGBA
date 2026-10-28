using System.Runtime.CompilerServices;

namespace AetherGba.Core.Common;

public static class BitUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBitSet(uint value, int bit) => ((value >> bit) & 1U) != 0;

    public static uint SetBit(uint value, int bit, bool set) =>
        set ? value | (1U << bit) : value & ~(1U << bit);

    public static uint RotateRight(uint value, int amount)
    {
        amount &= 31;
        if (amount == 0)
        {
            return value;
        }

        return (value >> amount) | (value << (32 - amount));
    }

    public static int SignExtend(int value, int bitCount)
    {
        var shift = 32 - bitCount;
        return (value << shift) >> shift;
    }
}