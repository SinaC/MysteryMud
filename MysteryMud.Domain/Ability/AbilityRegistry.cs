using MysteryMud.Core.Utilities;
using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Ability.Factories;
using MysteryMud.Domain.Action.Effect;

namespace MysteryMud.Domain.Ability;

public class AbilityRegistry : IAbilityRegistry
{
    private readonly Dictionary<int, AbilityRuntime> _abilitiesById = [];
    private readonly WordTrie<AbilityRuntime> _abilityWordTrie = new();

    private readonly IEffectRegistry _effectRegistry;
    private readonly IAbilityOutcomeResolverRegistry _abilityExecutionResolverRegistry;

    public AbilityRegistry(IEffectRegistry effectRegistry, IAbilityOutcomeResolverRegistry abilityExecutionResolverRegistry)
    {
        _effectRegistry = effectRegistry;
        _abilityExecutionResolverRegistry = abilityExecutionResolverRegistry;
    }

    public void Register(AbilityDefinition ability)
    {
        var abilityRuntime = AbilityRuntimeFactory.Create(_effectRegistry, _abilityExecutionResolverRegistry, ability);
        _abilitiesById.Add(ability.Id, abilityRuntime);
        _abilityWordTrie.Insert(ability.Name.ToLowerInvariant(), abilityRuntime);
    }

    public void Register(IEnumerable<AbilityDefinition> abilities)
    {
        foreach (var ability in abilities)
            Register(ability);
    }

    public bool TryGetRuntime(int abilityId, out AbilityRuntime? abilityRuntime)
        => _abilitiesById.TryGetValue(abilityId, out abilityRuntime);

    public bool StartsWith(string key, out AbilityRuntime? value)
        => _abilityWordTrie.StartsWith(key.ToLowerInvariant(), out value);
}
