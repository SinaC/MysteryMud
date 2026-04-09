using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;

namespace MysteryMud.Domain.Ability.Resolvers;

public class BerserkResolver : IAbilityExecutionResolver
{
    public AbilityExecutionResult Resolve(Entity caster, AbilityRuntime ability)
    {
        int chance = GetSkill(caster, ability); // however you store skills

        // fighting bonus
        if (caster.Has<CombatState>())
            chance += 10;

        // hp modifier
        ref var hp = ref caster.Get<Health>();
        int hpPercent = 100 * hp.Current / hp.Max;
        chance += 25 - hpPercent / 2;

        if (Random.Shared.Next(0,100) < chance)
        {
            return new AbilityExecutionResult
            {
                Success = true,
                Outcome = "OnSuccess",
                EffectIdsToApply = ability.EffectIds
            };
        }

        return new AbilityExecutionResult
        {
            Success = false,
            Outcome = "OnFailure",
            EffectIdsToApply = ability.FailureEffectIds
        };
    }

    private int GetSkill(Entity caster, AbilityRuntime ability)
    {
        // depends on your skill system
        //return caster.Get<SkillSet>().GetLevel(ability.Name);
        return 100; // TODO
    }
}