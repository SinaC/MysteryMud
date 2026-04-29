using DefaultEcs;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Infrastructure.Scheduler;

public struct ScheduledEvent
{
    public Entity Target;
    public ScheduledEventKind Kind;
    public long ExecuteAt;
}
