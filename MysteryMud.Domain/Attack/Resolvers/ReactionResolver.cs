using Arch.Core.Extensions;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Attack.Resolvers;

public class ReactionResolver
{
    private readonly IGameMessageService _msg;

    public ReactionResolver(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void Resolve(IIntentContainer intentContainer, AttackResult result)
    {
        // Buff procs reacting to the hit
        //TODO: HandleBuffProcs(world, resolved, ctx);

        // TODO
        //ref var budget = ref resolved.Target.Get<ReactionBudget>;
        //if (budget.Remaining <= 0) continue;

        // counterattack
        var trigger = false;

        // Parry -> guaranteed counter
        if (result.Result == AttackResultKind.Parry)
            trigger = true;
        // Hit -> chance to counter
        else if (result.Result == AttackResultKind.Hit)
        {
            ref var effectiveStats = ref result.Target.Get<EffectiveStats>();

            trigger = Random.Shared.NextDouble() < effectiveStats.CounterAttack;
        }

        if (!trigger)
            return;

        //budget.Remaining--;
        _msg.ToAll(result.Target).Act("{0} counterattack{0:v} {1:y} attack.").With(result.Target, result.Source);
        ref var counterAttackIntent = ref intentContainer.Attack.Add();
        counterAttackIntent.Cancelled = false;
        counterAttackIntent.Attacker = result.Target;
        counterAttackIntent.Target = result.Source;
        counterAttackIntent.RemainingHits = 1;
        counterAttackIntent.IsReaction = true;
        counterAttackIntent.IgnoreDefense = false;
    }
}
