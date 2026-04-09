using Arch.Core;

namespace MysteryMud.Domain.Ability.Resolvers;

public class DefaultAbilityExecutionResolver : IAbilityExecutionResolver
{
    public AbilityExecutionResult Resolve(Entity caster, AbilityRuntime ability)
    {
        return new AbilityExecutionResult
        {
            Success = true,
            EffectIdsToApply = ability.EffectIds
        };
    }
}
