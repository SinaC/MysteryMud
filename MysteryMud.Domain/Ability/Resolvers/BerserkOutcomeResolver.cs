using MysteryMud.Core.Random;
using MysteryMud.Domain.Components.Characters;
using TinyECS;

namespace MysteryMud.Domain.Ability.Resolvers;

public class BerserkOutcomeResolver : IAbilityOutcomeResolver
{
    private readonly World _world;
    private readonly IRandom _random;

    public BerserkOutcomeResolver(World world, IRandom random)
    {
        _random = random;
        _world = world;
    }

    public AbilityOutcomeResult Resolve(EntityId caster, AbilityRuntime ability)
    {
        int chance = GetSkill(caster, ability); // however you store skills

        // fighting bonus
        if (_world.Has<CombatState>(caster))
            chance += 10;

        // hp modifier
        ref var hp = ref _world.Get<Health>(caster);
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

    private int GetSkill(EntityId caster, AbilityRuntime ability)
    {
        // depends on your skill system
        //return caster.Get<SkillSet>().GetLevel(ability.Name);
        return 100; // TODO
    }
}