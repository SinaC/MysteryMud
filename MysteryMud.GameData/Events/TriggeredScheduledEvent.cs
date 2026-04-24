using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct TriggeredScheduledEvent
{
    public EntityId Effect;
    public ScheduledEventKind Kind;
}
