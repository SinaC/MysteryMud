using MysteryMud.Core.Contracts;
using MysteryMud.Core.Random;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Attack.Resolvers;

public class ReactionResolver : IReactionResolver
{
    private readonly IRandom _random;
    private readonly IGameMessageService _msg;

    public ReactionResolver(IRandom random, IGameMessageService msg)
    {
        _random = random;
        _msg = msg;
    }

    public void Resolve(IIntentContainer intentContainer, AttackResult result)
    {
        // Buff procs reacting to the hit
        //TODO: HandleBuffProcs(world, resolved, ctx);

        // TODO
        //ref var budget = ref resolved.Target.Get<ReactionBudget>;
        //if (budget.Remaining <= 0) continue;

        if (result.Target.Has<Casting>()) // no counterattack if casting
            return;

        // counterattack
        var trigger = false;

        // Parry -> guaranteed counter
        if (result.Result == AttackResultKind.Parry)
            trigger = true;
        // Hit -> chance to counter
        else if (result.Result == AttackResultKind.Hit)
        {
            ref var effectiveStats = ref result.Target.Get<EffectiveStats>();

            trigger = _random.Chance(effectiveStats.CounterAttack);
        }

        if (!trigger)
            return;

        //budget.Remaining--;
        _msg.ToAll(result.Target).Act("{0} counterattack{0:v} {1:y} attack.").With(result.Target, result.Source);
        ref var counterAttackIntent = ref intentContainer.Action.Add();
        counterAttackIntent.Kind = ActionKind.Attack;
        counterAttackIntent.Attack.Source = result.Target;
        counterAttackIntent.Attack.Target = result.Source;
        counterAttackIntent.Attack.RemainingHits = 1;
        counterAttackIntent.Attack.IsReaction = true;
        counterAttackIntent.Attack.IgnoreDefense = false;
    }
}
