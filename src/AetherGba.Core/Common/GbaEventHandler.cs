namespace AetherGba.Core.Common;

public sealed class GbaEventHandler
{
    public GbaEventHandler()
    {
        _eventStack[0] = new GbaEvent { Type = GbaEventType.Root, CycleDue = ulong.MaxValue, EventAction = _ => { } };
    }

    public ulong CurrentCycle;
    public ulong NextEventCycle = ulong.MaxValue;

    private readonly GbaEvent[] _eventStack = new GbaEvent[64]; //TODO: change type and ArraySize
    private int _eventStackPointer;

    public GbaEvent Pop()
    {
        var ev = _eventStack[_eventStackPointer--];
        NextEventCycle = _eventStack[_eventStackPointer].CycleDue;
        return ev;
    }

    public void RescheduleEventByType(GbaEventType eventType, ulong cycleDue)
    {
        var foundIdx = -1;
        for (int i = 0; i < _eventStackPointer; i++)
        {
            if (_eventStack[i].Type == eventType)
            {
                //found
                foundIdx = i;
                break;
            }
        }
        return;
    }

    public void CancelEventByType(GbaEventType eventType)
    {
        return;
    }

    public void AddScheduledEvent(GbaEvent eventType)
    {
        /*
        //fast path: new value is smallest so simple stack push
        if (eventType.CycleDue <= _eventStack[_eventStackPointer].CycleDue)
        {
            _eventStack[++_eventStackPointer] = eventType;
            NextEventCycle = _eventStack[_eventStackPointer].CycleDue;
            return;
        } //test with and without
        */

        int lo = 1, hi = _eventStackPointer + 1;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1; //div 2 aka binary search
            if (_eventStack[mid].CycleDue > eventType.CycleDue) // descending sort
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        // use memmove later? or array copy?
        for (int i = _eventStackPointer; i >= lo; i--)
            _eventStack[i + 1] = _eventStack[i];

        _eventStack[lo] = eventType;
        _eventStackPointer++;

        //keep NextEventCycle from going stale
        NextEventCycle = _eventStack[_eventStackPointer].CycleDue;
    }
}