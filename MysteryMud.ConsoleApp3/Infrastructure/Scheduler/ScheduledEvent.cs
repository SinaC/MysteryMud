using Arch.Core;
using MysteryMud.ConsoleApp3.Core.Eventing;

namespace MysteryMud.ConsoleApp3.Infrastructure.Scheduler;

public struct ScheduledEvent
{
    public long ExecuteAt;
    public ScheduledEventType Type;
    public Entity Target;
}
