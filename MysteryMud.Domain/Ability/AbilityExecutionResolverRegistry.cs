using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Ability.Resolvers;

namespace MysteryMud.Domain.Ability;

public class AbilityExecutionResolverRegistry
{
    private readonly Dictionary<string, RegisteredAbilityExecutionResolver> _resolverByName = new (StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, RegisteredAbilityExecutionResolver> _resolverById = [];

    public void Register(string name, IAbilityExecutionResolver resolver)
    {
        var entry = new RegisteredAbilityExecutionResolver
        {
            Id = name.ComputeUniqueId(),
            Resolver = resolver
        };

        _resolverByName.Add(name, entry);
        _resolverById.Add(entry.Id, entry);
    }

    public bool TryGetResolver(string name, out RegisteredAbilityExecutionResolver? resolver)
        => _resolverByName.TryGetValue(name, out resolver);

    public bool TryGetResolver(int id, out RegisteredAbilityExecutionResolver? resolver)
        => _resolverById.TryGetValue(id, out resolver);
}

public sealed class RegisteredAbilityExecutionResolver
{
    public required int Id { get; init; }
    public required IAbilityExecutionResolver Resolver { get; init; }
}
