using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Domain.Components.Characters;
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

    // pass 2: called after ActionOrchestrator
    // handles aggression from outcomes: resisted effects, procs, reactions
    public void Tick(GameState state)
    {
        foreach (ref var evt in _aggressed.GetAll())
        {
            EnterCombat(evt.Source, evt.Target);
        }
    }

    private void EnterCombat(Entity source, Entity target)
    {
        if (!CharacterHelpers.IsAlive(source, target)) return;

        if (!source.Has<CombatState>())
        {
            source.Add(new CombatState { Target = target, RoundDelay = 0 });
            source.Add<NewCombatantTag>();
        }

        if (!target.Has<CombatState>())
        {
            target.Add(new CombatState { Target = source, RoundDelay = 1 });
            target.Add<NewCombatantTag>();
        }
    }
}
