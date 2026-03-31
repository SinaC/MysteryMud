using Arch.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Core.Scheduler;

public interface IScheduler
{
    void Schedule(Entity entity, ScheduledEventKind kind, long executedAt);
    void Process(GameState state, IEventBuffer<TriggeredScheduledEvent> triggeredScheduledEvents);
}
