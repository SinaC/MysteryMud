using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Scheduler;
using MysteryMud.Domain.Helpers;
using TinyECS;

namespace MysteryMud.Domain.Systems;

public class ScheduleSystem
{
    private readonly World _world;
    private readonly ILogger _logger;
    private readonly IScheduler _scheduler;
    private readonly IIntentContainer _intentContainer;

    public ScheduleSystem(World world, ILogger logger, IScheduler scheduler, IIntentContainer intentContainer)
    {
        _world = world;
        _logger = logger;
        _scheduler = scheduler;
        _intentContainer = intentContainer;
    }

    public void Tick(GameState state)
    {
        foreach (var intent in _intentContainer.ScheduleSpan)
        {
            _logger.LogDebug("[{system}]: schedule intent {effectName} kind {kind} execute at {executeAt}", nameof(ScheduleSystem), EntityHelpers.DebugName(_world, intent.Effect), intent.Kind, intent.ExecuteAt);

            _scheduler.Schedule(state, intent.Effect, intent.Kind, intent.ExecuteAt);
        }
    }
}
