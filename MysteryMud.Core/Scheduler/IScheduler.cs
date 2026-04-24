using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Core.Scheduler;

public interface IScheduler
{
    void Schedule(GameState state, EntityId entity, ScheduledEventKind kind, long executeAt);
    void Process(GameState state);
}
