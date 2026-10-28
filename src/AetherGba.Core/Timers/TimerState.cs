namespace AetherGba.Core.Timers;

public struct TimerState
{
    //The clock cycle at the last snapshot
    public ulong SnapshotCycle;
    public ulong NextOverflowCycle;

    public ushort Control;
    public ushort Reload;
    //The Counter's value when last snapshot taken
    public ushort CounterSnapshot;

    public byte PrescalerShift;

    public bool Enabled;
    public bool Cascade;
    public bool IrqEnabled;
}