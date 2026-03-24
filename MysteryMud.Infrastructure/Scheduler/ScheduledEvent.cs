using Arch.Core;
using MysteryMud.Core.Scheduler;

namespace MysteryMud.Infrastructure.Scheduler;

public struct ScheduledEvent
{
    public long ExecuteAt;
    public ScheduledEventType Type;
    public Entity Target;
}
