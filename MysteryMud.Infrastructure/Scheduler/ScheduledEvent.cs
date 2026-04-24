using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Infrastructure.Scheduler;

public struct ScheduledEvent
{
    public EntityId Target;
    public ScheduledEventKind Kind;
    public long ExecuteAt;
}
