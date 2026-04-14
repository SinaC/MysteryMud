using MysteryMud.Domain.Ability.Resolvers;

namespace MysteryMud.Domain.Ability;

public sealed class RegisteredAbilityOutcomeResolver
{
    public required int Id { get; init; }
    public required IAbilityOutcomeResolver Resolver { get; init; }
}
