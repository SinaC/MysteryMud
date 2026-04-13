using MysteryMud.Domain.Ability.Resolvers;

namespace MysteryMud.Domain.Ability;

public interface IAbilityOutcomeResolverRegistry
{
    void Register(string name, IAbilityOutcomeResolver resolver);

    bool TryGetResolver(string name, out RegisteredAbilityOutcomeResolver? resolver);
    bool TryGetResolver(int id, out RegisteredAbilityOutcomeResolver? resolver);
}