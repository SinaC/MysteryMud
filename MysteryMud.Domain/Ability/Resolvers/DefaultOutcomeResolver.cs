using DefaultEcs;

namespace MysteryMud.Domain.Ability.Resolvers;

public class DefaultOutcomeResolver : IAbilityOutcomeResolver
{
    public AbilityOutcomeResult Resolve(Entity caster, AbilityRuntime ability)
    {
        return new AbilityOutcomeResult
        {
            Success = true
        };
    }
}
