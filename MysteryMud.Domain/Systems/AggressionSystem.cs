using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Events;
using TinyECS;

namespace MysteryMud.Domain.Systems;

public class AggressionSystem
{
    private readonly World _world;
    private readonly IEventBuffer<AggressedEvent> _aggressed;

    public AggressionSystem(World world, IEventBuffer<AggressedEvent> aggressed)
    {
        _world = world;
        _aggressed = aggressed;
    }

    // handles aggression from outcomes: resisted effects, procs, reactions
    public void Tick(GameState state)
    {
        foreach (ref var evt in _aggressed.GetAll())
        {
            CombatHelpers.EnterCombat(_world, state, evt.Source, evt.Target);
        }
    }
}
