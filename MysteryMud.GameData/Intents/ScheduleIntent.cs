using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct ScheduleIntent
{
    public EntityId Effect;
    public ScheduledEventKind Kind;
    public long ExecuteAt;
}
