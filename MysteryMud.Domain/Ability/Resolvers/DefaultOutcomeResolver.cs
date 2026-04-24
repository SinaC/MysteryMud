using TinyECS;

namespace MysteryMud.Domain.Ability.Resolvers;

public class DefaultOutcomeResolver : IAbilityOutcomeResolver
{
    public AbilityOutcomeResult Resolve(EntityId caster, AbilityRuntime ability)
    {
        return new AbilityOutcomeResult
        {
            Success = true
        };
    }
}
