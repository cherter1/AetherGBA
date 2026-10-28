using System.Runtime.CompilerServices;
using AetherGba.Core.Memory;

namespace AetherGba.Core.Interrupts;

public sealed class InterruptController(GbaMemory memory)
{
    public bool ServiceIrq { get; private set; }

    public bool ShouldServiceIrq(bool irqDisabled) =>
        !irqDisabled && memory.Io.REG_IME && (memory.Io.REG_IE & memory.Io.REG_IF) != 0;

    public void Request(InterruptType interrupt)
    {
        memory.Io.REG_IF |= (ushort)interrupt;
        UpdateServiceIrq();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateServiceIrq()
    {
        ServiceIrq = memory.Io.REG_IME && (memory.Io.REG_IE & memory.Io.REG_IF) != 0;
    }
}