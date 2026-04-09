using Arch.Core;

namespace MysteryMud.Domain.Ability.Resolvers;

public interface IAbilityExecutionResolver
{
    AbilityExecutionResult Resolve(Entity caster, AbilityRuntime ability);
}