using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Systems;

public class AutoAttackSystem
{
    private const int DefaultHits = 1; // e.g., base autoattack hits

    private readonly IIntentContainer _intentContainer;
    private readonly EntitySet _inCombatEntitySet;

    public AutoAttackSystem(World world, IIntentContainer intentContainer)
    {
        _intentContainer = intentContainer;
        _inCombatEntitySet = world
            .GetEntities()
            .With<CombatState>()
            .With<EffectiveStats>()
            .Without<DeadTag>() // no autoattack if dead or casting
            .Without<Casting>()
            .AsSet();
    }

    public void Tick(GameState state)
    {
        foreach(var entity in _inCombatEntitySet.GetEntities())
        {
            ref var combat = ref entity.Get<CombatState>();
            ref var stats = ref entity.Get<EffectiveStats>();

            var target = combat.Target;
            if (target.Has<DeadTag>())
            {
                // Target is gone, exit combat
                entity.Remove<CombatState>();
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
            int hits = Math.Max(DefaultHits, stats.AttackCount); // TODO

            // Add attack intent
            ref var attackIntent = ref _intentContainer.Action.Add();
            attackIntent.Kind = ActionKind.Attack;
            attackIntent.Attack.Source = entity;
            attackIntent.Attack.Target = target;
            attackIntent.Attack.RemainingHits = hits;
            attackIntent.Attack.IsReaction = false; // autoattack, not a reaction
            attackIntent.Attack.IgnoreDefense = false; // autoattacks are affected by defense
            attackIntent.Cancelled = false;

            // Apply lag before next attack
            combat.RoundDelay = 2; // example: 2 ticks
        }
    }
}
