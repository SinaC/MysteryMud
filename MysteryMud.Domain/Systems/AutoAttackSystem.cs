using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Intent;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Systems;

public class AutoAttackSystem
{
    private const int defaultHits = 1; // e.g., base autoattack hits

    private readonly IIntentContainer _intentContainer;

    public AutoAttackSystem(IIntentContainer intentContainer)
    {
        this._intentContainer = intentContainer;
    }

    public void Tick(GameState state)
    {
        var query = new QueryDescription()
            .WithAll<CombatState, EffectiveStats>()
            .WithNone<Dead>();
        state.World.Query(query, (Entity actor, ref CombatState combat, ref EffectiveStats stats) =>
        {
            var target = combat.Target;
            if (target.Has<Dead>())
            {
                // Target is gone, exit combat
                actor.Remove<CombatState>();
                return;
                // TODO: Alternatively, could try to select new target here instead of exiting combat
            }

            // TODO: check target is in combat with actor, or if target is out of range, etc.

            if (combat.RoundDelay > 0)
            {
                combat.RoundDelay--;
                return;
            }

            // Determine number of attacks (multi-hit)
            int hits = Math.Max(defaultHits, stats.AttackCount); // TODO

            ref var attackIntent = ref _intentContainer.Attack.Add();
            attackIntent.Attack.Attacker = actor;
            attackIntent.Attack.Target = target;
            attackIntent.Attack.RemainingHits = hits;
            attackIntent.Attack.IsReaction = false; // autoattack, not a reaction
            attackIntent.Attack.IgnoreDefense = false; // autoattacks are affected by defense

            // Apply lag before next attack
            combat.RoundDelay = 2; // example: 2 ticks
        });
    }
}
