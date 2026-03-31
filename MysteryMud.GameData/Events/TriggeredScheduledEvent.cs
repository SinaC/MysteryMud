using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct TriggeredScheduledEvent
{
    public Entity Effect;
    public ScheduledEventKind Kind;
}
