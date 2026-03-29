using Arch.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.GameData.Events;

namespace MysteryMud.Core.Scheduler;

public interface IScheduler
{
    void Schedule(Entity entity, ScheduledEventType type, long executedAt);
    void Process(GameState state, IEventBuffer<DotTriggeredEvent> dots, IEventBuffer<HotTriggeredEvent> hots, IEventBuffer<EffectExpiredEvent> expired);
}
