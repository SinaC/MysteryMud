using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public class AggressionSystem
{
    private readonly ICombatService _combatService;
    private readonly IEventBuffer<AggressedEvent> _aggressed;

    public AggressionSystem(ICombatService combatService, IEventBuffer<AggressedEvent> aggressed)
    {
        _combatService = combatService;
        _aggressed = aggressed;
    }

    // handles aggression from outcomes: resisted effects, procs, reactions
    public void Tick(GameState state)
    {
        foreach (ref var evt in _aggressed.GetAll())
        {
            _combatService.EnterCombat(state, evt.Source, evt.Target);
        }
    }
}
