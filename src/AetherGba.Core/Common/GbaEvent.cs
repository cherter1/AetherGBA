using AetherGba.Core.Memory;

namespace AetherGba.Core.Common;

public sealed class GbaEvent
{
    public GbaEventType Type;
    public ulong CycleDue;
    public Action<GbaBus> EventAction; //TODO: Temporary depending on other events
}