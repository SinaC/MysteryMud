using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Core.Scheduler;

public interface IScheduler
{
    void Schedule(GameState state, Entity entity, ScheduledEventKind kind, long executeAt);
    void Process(GameState state);
}
