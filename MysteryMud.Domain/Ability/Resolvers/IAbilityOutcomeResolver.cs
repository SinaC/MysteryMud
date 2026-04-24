using TinyECS;

namespace MysteryMud.Domain.Ability.Resolvers;

public interface IAbilityOutcomeResolver
{
    AbilityOutcomeResult Resolve(EntityId caster, AbilityRuntime ability);
}