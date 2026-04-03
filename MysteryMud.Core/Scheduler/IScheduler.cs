using Arch.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Core.Scheduler;

public interface IScheduler
{
    void Schedule(GameState state, Entity entity, ScheduledEventKind kind, long executeAt);
    void Process(GameState state, IEventBuffer<TriggeredScheduledEvent> triggeredScheduledEvents);
}
