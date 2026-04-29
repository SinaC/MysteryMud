using DefaultEcs;
using MysteryMud.Core.Random;
using MysteryMud.Domain.Components.Characters;

namespace MysteryMud.Domain.Ability.Resolvers;

public class BerserkOutcomeResolver : IAbilityOutcomeResolver
{
    private readonly IRandom _random;

    public BerserkOutcomeResolver(IRandom random)
    {
        _random = random;
    }

    public AbilityOutcomeResult Resolve(Entity caster, AbilityRuntime ability)
    {
        int chance = GetSkill(caster, ability); // however you store skills

        // fighting bonus
        if (caster.Has<CombatState>())
            chance += 10;

        // hp modifier
        ref var hp = ref caster.Get<Health>();
        int hpPercent = 100 * hp.Current / hp.Max;
        chance += 25 - hpPercent / 2;

        if (_random.Chance(chance))
        {
            return new AbilityOutcomeResult
            {
                Success = true,
                Outcome = "OnSuccess",
            };
        }

        return new AbilityOutcomeResult
        {
            Success = false,
            Outcome = "OnFailure",
        };
    }

    private int GetSkill(Entity caster, AbilityRuntime ability)
    {
        // depends on your skill system
        //return caster.Get<SkillSet>().GetLevel(ability.Name);
        return 100; // TODO
    }
}