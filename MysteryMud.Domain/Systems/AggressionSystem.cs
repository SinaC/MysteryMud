using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public class AggressionSystem
{
    private readonly IEventBuffer<AggressedEvent> _aggressed;

    public AggressionSystem(IEventBuffer<AggressedEvent> aggressed)
    {
        _aggressed = aggressed;
    }

    // handles aggression from outcomes: resisted effects, procs, reactions
    public void Tick(GameState state)
    {
        foreach (ref var evt in _aggressed.GetAll())
        {
            CombatHelpers.EnterCombat(state, evt.Source, evt.Target);
        }
    }
}
