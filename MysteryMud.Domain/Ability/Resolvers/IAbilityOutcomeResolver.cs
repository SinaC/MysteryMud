using DefaultEcs;

namespace MysteryMud.Domain.Ability.Resolvers;

public interface IAbilityOutcomeResolver
{
    AbilityOutcomeResult Resolve(Entity caster, AbilityRuntime ability);
}