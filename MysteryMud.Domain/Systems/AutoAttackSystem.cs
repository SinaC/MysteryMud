using MysteryMud.Core;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Systems;

public class AutoAttackSystem
{
    private const int DefaultHits = 1; // e.g., base autoattack hits

    private readonly World _world;
    private readonly IIntentContainer _intentContainer;

    public AutoAttackSystem(World world, IIntentContainer intentContainer)
    {
        _world = world;
        _intentContainer = intentContainer;
    }

    private static readonly QueryDescription _inCombatButNotCasting = new QueryDescription()
        .WithAll<CombatState, EffectiveStats>()
        .WithNone<Dead, Casting>(); // no autoattack if dead or casting

    public void Tick(GameState state)
    {
        _world.Query(_inCombatButNotCasting, (EntityId actor,
            ref CombatState combat,
            ref EffectiveStats stats) =>
        {
            var target = combat.Target;
            if (_world.Has<Dead>(target))
            {
                // Target is gone, exit combat
                _world.Remove<CombatState>(actor);
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
            attackIntent.Attack.Source = actor;
            attackIntent.Attack.Target = target;
            attackIntent.Attack.RemainingHits = hits;
            attackIntent.Attack.IsReaction = false; // autoattack, not a reaction
            attackIntent.Attack.IgnoreDefense = false; // autoattacks are affected by defense
            attackIntent.Cancelled = false;

            // Apply lag before next attack
            combat.RoundDelay = 2; // example: 2 ticks
        });
    }
}
