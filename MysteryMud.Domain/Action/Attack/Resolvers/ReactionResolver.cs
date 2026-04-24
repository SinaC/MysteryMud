using MysteryMud.Core.Contracts;
using MysteryMud.Core.Random;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Action.Attack.Resolvers;

public class ReactionResolver : IReactionResolver
{
    private readonly World _world;
    private readonly IRandom _random;
    private readonly IGameMessageService _msg;

    public ReactionResolver(World world, IRandom random, IGameMessageService msg)
    {
        _world = world;
        _random = random;
        _msg = msg;
    }

    public void Resolve(IIntentContainer intentContainer, AttackResult result)
    {
        var target = result.Target;
        var source = result.Source;
        // Buff procs reacting to the hit
        //TODO: HandleBuffProcs(world, resolved, ctx);

        // TODO
        //ref var budget = ref resolved.Target.Get<ReactionBudget>;
        //if (budget.Remaining <= 0) continue;

        if (_world.Has<Casting>(target)) // no counterattack if casting
            return;

        // counterattack
        var trigger = false;

        // Parry -> guaranteed counter
        if (result.Result == AttackResultKind.Parry)
            trigger = true;
        // Hit -> chance to counter
        else if (result.Result == AttackResultKind.Hit)
        {
            ref var effectiveStats = ref _world.Get<EffectiveStats>(target);

            trigger = _random.Chance(effectiveStats.CounterAttack);
        }

        if (!trigger)
            return;

        //budget.Remaining--;
        _msg.ToAll(target).Act("{0} counterattack{0:v} {1:y} attack.").With(target, source);
        ref var counterAttackIntent = ref intentContainer.Action.Add();
        counterAttackIntent.Kind = ActionKind.Attack;
        counterAttackIntent.Attack.Source = target;
        counterAttackIntent.Attack.Target = source;
        counterAttackIntent.Attack.RemainingHits = 1;
        counterAttackIntent.Attack.IsReaction = true;
        counterAttackIntent.Attack.IgnoreDefense = false;
    }
}
