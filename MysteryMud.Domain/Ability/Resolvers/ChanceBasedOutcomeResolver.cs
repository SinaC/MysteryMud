using MysteryMud.Core.Random;
using TinyECS;

namespace MysteryMud.Domain.Ability.Resolvers;

public class ChanceBasedOutcomeResolver : IAbilityOutcomeResolver
{
    private readonly World _world;
    private readonly IRandom _random;

    public ChanceBasedOutcomeResolver(World world, IRandom random)
    {
        _world = world;
        _random = random;
    }

    public AbilityOutcomeResult Resolve(EntityId caster, AbilityRuntime ability)
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


    private int GetSkill(EntityId caster, AbilityRuntime ability)
    {
        // depends on your skill system
        //return caster.Get<SkillSet>().GetLevel(ability.Name);
        return 100; // TODO
    }
}
