using Arch.Core;

namespace MysteryMud.ConsoleApp3.Core.Eventing;

public struct ScheduledEvent
{
    public long ExecuteAt;
    public ScheduledEventType Type;
    public Entity Target;
}
