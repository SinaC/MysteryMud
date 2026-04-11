using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Ability.Resolvers;

namespace MysteryMud.Domain.Ability;

public class AbilityOutcomeResolverRegistry
{
    private readonly Dictionary<string, RegisteredAbilityOutcomeResolver> _resolverByName = new (StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, RegisteredAbilityOutcomeResolver> _resolverById = [];

    public void Register(string name, IAbilityOutcomeResolver resolver)
    {
        var entry = new RegisteredAbilityOutcomeResolver
        {
            Id = name.ComputeUniqueId(),
            Resolver = resolver
        };

        _resolverByName.Add(name, entry);
        _resolverById.Add(entry.Id, entry);
    }

    public bool TryGetResolver(string name, out RegisteredAbilityOutcomeResolver? resolver)
        => _resolverByName.TryGetValue(name, out resolver);

    public bool TryGetResolver(int id, out RegisteredAbilityOutcomeResolver? resolver)
        => _resolverById.TryGetValue(id, out resolver);
}

public sealed class RegisteredAbilityOutcomeResolver
{
    public required int Id { get; init; }
    public required IAbilityOutcomeResolver Resolver { get; init; }
}
