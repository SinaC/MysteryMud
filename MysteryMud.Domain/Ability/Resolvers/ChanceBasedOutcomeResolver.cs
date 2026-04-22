using Arch.Core;
using MysteryMud.Core.Random;

namespace MysteryMud.Domain.Ability.Resolvers;

public class ChanceBasedOutcomeResolver : IAbilityOutcomeResolver
{
    private readonly IRandom _random;

    public ChanceBasedOutcomeResolver(IRandom random)
    {
        _random = random;
    }

    public AbilityOutcomeResult Resolve(Entity caster, AbilityRuntime ability)
    {
        int chance = GetSkill(caster, ability);

        if (_random.Chance(chance))
        {
            return new AbilityOutcomeResult
            {
                Success = true,
                Outcome = "OnSuccess"
            };
        }

        return new AbilityOutcomeResult
        {
            Success = false,
            Outcome = "OnFailure"
        };
    }


    private int GetSkill(Entity caster, AbilityRuntime ability)
    {
        // depends on your skill system
        //return caster.Get<SkillSet>().GetLevel(ability.Name);
        return 100; // TODO
    }
}
