using Arch.Core;

namespace MysteryMud.Domain.Ability.Resolvers;

public class ChanceBasedOutcomeResolver : IAbilityOutcomeResolver
{
    public AbilityOutcomeResult Resolve(Entity caster, AbilityRuntime ability)
    {
        int chance = GetSkill(caster, ability);

        if (Random.Shared.Next(0, 100) < chance)
        {
            return new AbilityOutcomeResult
            {
                Success = true,
                Outcome = "OnSuccess",
                EffectIdsToApply = ability.EffectIds
            };
        }

        return new AbilityOutcomeResult
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
