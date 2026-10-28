using AetherGba.Core.Common;

namespace AetherGba.Core.Tests.Common;

public sealed class BitUtilsTests
{
    [Fact]
    public void SignExtend_ExtendsNegativeValue()
    {
        var result = BitUtils.SignExtend(0b1111, 4);
        Assert.Equal(-1, result);
    }

    [Fact]
    public void RotateRight_WrapsBits()
    {
        var result = BitUtils.RotateRight(0x80000001, 1);
        Assert.Equal(0xC0000000U, result);
    }
}
