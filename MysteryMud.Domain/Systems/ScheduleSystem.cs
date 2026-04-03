using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Scheduler;
using MysteryMud.Domain.Extensions;

namespace MysteryMud.Domain.Systems;

public class ScheduleSystem
{
    private readonly ILogger _logger;
    private readonly IScheduler _scheduler;
    private readonly IIntentContainer _intentContainer;

    public ScheduleSystem(ILogger logger, IScheduler scheduler, IIntentContainer intentContainer)
    {
        _logger = logger;
        _scheduler = scheduler;
        _intentContainer = intentContainer;
    }

    public void Tick(GameState state)
    {
        foreach (var intent in _intentContainer.ScheduleSpan)
        {
            _logger.LogDebug("[{system}]: schedule intent {effectName} kind {kind} execute at {executeAt}", nameof(ScheduleSystem), intent.Effect.DebugName, intent.Kind, intent.ExecuteAt);

            _scheduler.Schedule(state, intent.Effect, intent.Kind, intent.ExecuteAt);
        }
    }
}
