using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct ScheduleIntent
{
    public Entity Effect;
    public ScheduledEventKind Kind;
    public long ExecuteAt;
}
