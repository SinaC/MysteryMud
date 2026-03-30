using Arch.Core.Extensions;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Combat.Resolvers;

public class ReactionResolver
{
    private readonly IGameMessageService _msg;

    public ReactionResolver(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void Resolve(IIntentContainer intentContainer, AttackResolved resolved)
    {
        // Buff procs reacting to the hit
        //TODO: HandleBuffProcs(world, resolved, ctx);

        // TODO
        //ref var budget = ref resolved.Target.Get<ReactionBudget>;
        //if (budget.Remaining <= 0) continue;

        // counterattack
        var trigger = false;

        // Parry -> guaranteed counter
        if (resolved.Result == AttackResults.Parry)
            trigger = true;
        // Hit -> chance to counter
        else if (resolved.Result == AttackResults.Hit && resolved.SourceType == DamageSourceTypes.Hit)
        {
            ref var effectiveStats = ref resolved.Target.Get<EffectiveStats>();

            trigger = Random.Shared.NextDouble() < effectiveStats.CounterAttack;
        }

        if (!trigger)
            return;

        //budget.Remaining--;
        _msg.ToRoom(resolved.Target).Act("{0} counterattacks {1:y} attack.").With(resolved.Target, resolved.Source);
        ref var attackIntent = ref intentContainer.Attack.Add();
        attackIntent.Attacker = resolved.Target;
        attackIntent.Target = resolved.Source;
        attackIntent.RemainingHits = 1;
        attackIntent.IsReaction = true;
    }
}
