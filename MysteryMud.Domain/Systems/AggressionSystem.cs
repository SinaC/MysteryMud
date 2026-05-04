using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public class AggressionSystem
{
    private readonly ILogger _logger;
    private readonly ICombatService _combatService;
    private readonly IEventBuffer<AggressedEvent> _aggressed;

    public AggressionSystem(ILogger logger, ICombatService combatService, IEventBuffer<AggressedEvent> aggressed)
    {
        _logger = logger;
        _combatService = combatService;
        _aggressed = aggressed;
    }

    // handles aggression from outcomes: resisted effects, procs, reactions
    public void Tick(GameState state)
    {
        foreach (ref var evt in _aggressed.GetAll())
        {
            if (CharacterHelpers.SameRoom(evt.Source, evt.Target))
                _combatService.EnterCombat(state, evt.Source, evt.Target);
            else
                _logger.LogError("AggressionSystem: Source {Source} and Target {Target} are not in the same room, cannot enter combat", evt.Source, evt.Target);
        }
    }
}
