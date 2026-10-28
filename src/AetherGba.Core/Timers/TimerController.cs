using System.Runtime.CompilerServices;
using AetherGba.Core.Interrupts;

namespace AetherGba.Core.Timers;

public sealed class TimerController
{
    private readonly InterruptController _interrupts;

    private ulong _currentCycle;
    private ulong _nextTimerOverflow = ulong.MaxValue;
    private readonly TimerState[] _timers = new TimerState[4];

    public TimerController(InterruptController interrupts)
    {
        _interrupts = interrupts;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int cycles)
    {
        _currentCycle += (uint)cycles;
        if (_currentCycle >= _nextTimerOverflow)
        {
            ProcessTimerOverflows();
        }
    }

    public ushort ReadCounter(int index)
    {
        return GetCurrentCounter(ref _timers[index]);
    }

    public void WriteReload(int index, ushort value)
    {
        _timers[index].Reload = value;
    }

    public ushort ReadControl(int index)
    {
        return _timers[index].Control;
    }

    public void WriteControl(int index, ushort value)
    {
        ref TimerState timer = ref _timers[index];

        bool wasEnabled = timer.Enabled;
        bool setEnabled = (value & 0x80) != 0;

        bool oldCascade = timer.Cascade;
        bool newCascade = index != 0 && (value & 0x4) != 0;

        byte newPrescalerShift = (value & 0b11) switch
        {
            0 => 0, //2Pow0 aka every 1 cycles
            1 => 6, //2Pow6 aka every 64 cycles
            2 => 8, //2Pow8 aka every 256 cycles
            3 => 10, //2Pow10 aka every 1024 cycles
            _ => 0
        };

        timer.Control = (ushort)(value & 0xc7); //preserve full control reg value for easy read
        timer.IrqEnabled = (value & 0x40) != 0; //bit 6 IRQ

        bool overflowScheduleChanged = false;
        if (!wasEnabled && setEnabled) //Turn Timer On
        {
            timer.Enabled = true;
            timer.Cascade = newCascade;
            timer.PrescalerShift = newPrescalerShift;
            timer.CounterSnapshot = timer.Reload;
            timer.SnapshotCycle = _currentCycle;

            SetNextTimerOverflowCycle(ref timer);
            overflowScheduleChanged = true;
        }
        else if (wasEnabled && !setEnabled) //Turn Timer Off
        {
            timer.CounterSnapshot = GetCurrentCounter(ref timer);
            timer.SnapshotCycle = _currentCycle;
            timer.Enabled = false;
            timer.Cascade = newCascade;
            timer.PrescalerShift = newPrescalerShift;
            timer.NextOverflowCycle = ulong.MaxValue;

            overflowScheduleChanged = true;
        }
        else if (wasEnabled) //timer stays enabled
        {
            bool timingChanged = oldCascade != newCascade || timer.PrescalerShift != newPrescalerShift;
            if (timingChanged)
            {
                //Counter does not change but start timing from current globalCycle
                timer.CounterSnapshot = GetCurrentCounter(ref timer);
                timer.SnapshotCycle = _currentCycle;
            }

            timer.Cascade = newCascade;
            timer.PrescalerShift = newPrescalerShift;

            if (timingChanged)
            {
                SetNextTimerOverflowCycle(ref timer);
                overflowScheduleChanged = true;
            }
        }
        else //timer stays disabled
        {
            timer.Cascade = newCascade;
            timer.PrescalerShift = newPrescalerShift;
            timer.NextOverflowCycle = ulong.MaxValue;
        }

        if (overflowScheduleChanged) RecalculateNextTimerOverflow();
    }

    private static void SetNextTimerOverflowCycle(ref TimerState timer)
    {
        if (!timer.Enabled || timer.Cascade) //not enabled never overflows, cascade never overflows this way it will be handled when a non cascade timer overflows
        {
            timer.NextOverflowCycle = ulong.MaxValue; //ulong max is effectively never
            return;
        }

        ulong ticksToOverflow = 0x10000ul - timer.CounterSnapshot;
        timer.NextOverflowCycle = timer.SnapshotCycle + (ticksToOverflow << timer.PrescalerShift); //TicksToOverflow * PrescalerVal(2PowPrescaler) + snapshotCycle = cycle that this timer overflows again
    }

    private ushort GetCurrentCounter(ref TimerState timer)
    {
        if (!timer.Enabled || timer.Cascade) //disabled and cascade timers have snapshots up to date
        {
            return timer.CounterSnapshot;
        }

        ulong elapsedCycles = _currentCycle - timer.SnapshotCycle; //num cycles since snapshot taken
        ulong ticks = elapsedCycles >> timer.PrescalerShift; // elapsed / prescalerVal (2PowPrescalerShift) gives Ticks

        return (ushort)(timer.CounterSnapshot + ticks);
    }

    private void ProcessTimerOverflows()
    {
        for (int i = 0; i < 4; i++)
        {
            ref TimerState timer = ref _timers[i];

            if (!timer.Enabled || timer.Cascade || timer.NextOverflowCycle > _currentCycle)
            {
                continue;
            }
            ProcessClockTimerOverflow(i);
        }
        RecalculateNextTimerOverflow();
    }

    private void ProcessClockTimerOverflow(int index)
    {
        ref TimerState timer = ref _timers[index];

        ulong termCycles = (0x10000ul - timer.Reload) << timer.PrescalerShift; // timerTerm * PrescalerVal(cycles per increment)(2PowPrescaler) = totalCycles from start to overflow

        ulong additionalOverflows = (_currentCycle - timer.NextOverflowCycle) / termCycles; //num overflows to apply on top of implied first overflow
        ulong finalOverflowCycle = timer.NextOverflowCycle + (additionalOverflows * termCycles); //cycle that the final handled overflow occurred

        timer.CounterSnapshot = timer.Reload;
        timer.SnapshotCycle = finalOverflowCycle;
        timer.NextOverflowCycle = finalOverflowCycle + termCycles; //next overflow is one full term after the last overflow

        if (timer.IrqEnabled)
        {
            _interrupts.Request((InterruptType)((ushort)InterruptType.Timer0 << index));
        }

        if (index < 3)
        {
            ApplyCascadeTicks(index + 1, additionalOverflows + 1); // cascade next timer and add back the implied overflow to the additionalOverflows
        }
    }

    private void ApplyCascadeTicks(int index, ulong ticks)
    {
        while (true) //iteration loop instead of tail recursion
        {
            ref TimerState timer = ref _timers[index];
            if (!timer.Enabled || !timer.Cascade || ticks == 0)
            {
                return;
            }

            ulong counter = timer.CounterSnapshot;
            ulong ticksToOverflow = 0x10000ul - counter;

            if (ticks < ticksToOverflow) //Not overflowing apply Ticks
            {
                timer.CounterSnapshot = (ushort)(counter + ticks);
                return;
            }

            //handle timerOverflow
            ulong remainingTicks = ticks - ticksToOverflow; //Ticks leftover to apply after first overflow
            ulong term = 0x10000ul - timer.Reload; //full term of timer from start to overflow

            ulong overflowCount = 1 + (remainingTicks / term); //num overflows to apply

            timer.CounterSnapshot = (ushort)(timer.Reload + (remainingTicks % term)); //add remaining ticks to reload to get snapshot mod to handle multiple overflows
            if (timer.IrqEnabled)
            {
                _interrupts.Request((InterruptType)((ushort)InterruptType.Timer0 << index));
            }

            if (index < 3)
            {
                index += 1; //index next cascade timer
                ticks = overflowCount; //apply num overflows as ticks to next cascade timer
                continue; //apply cascade to next timer
            }

            break; //return when no more timers to cascade
        }
    }

    private void RecalculateNextTimerOverflow()
    {
        ulong next = ulong.MaxValue;
        for (int i = 0; i < 4; i++)
        {
            ref TimerState timer = ref _timers[i];

            if (timer is { Enabled: true, Cascade: false } && timer.NextOverflowCycle < next) //disabled and cascading timers can never be the next timer to overflow
            {
                next = timer.NextOverflowCycle;
            }
        }
        _nextTimerOverflow = next;
    }
}