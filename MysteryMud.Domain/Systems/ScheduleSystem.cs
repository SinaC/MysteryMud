using MysteryMud.Core;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Scheduler;

namespace MysteryMud.Domain.Systems;

public class ScheduleSystem
{
    private readonly IScheduler _scheduler;
    private readonly IIntentContainer _intentContainer;

    public ScheduleSystem(IScheduler scheduler, IIntentContainer intentContainer)
    {
        _scheduler = scheduler;
        _intentContainer = intentContainer;
    }

    public void Tick(GameState state)
    {
        foreach (var intent in _intentContainer.ScheduleSpan)
        {
            _scheduler.Schedule(intent.Effect, intent.Kind, intent.ExecuteAt);
        }
    }
}
