using MysteryMud.Core.Trie;
using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Ability.Factories;
using MysteryMud.Domain.Action.Effect;

namespace MysteryMud.Domain.Ability;

public class AbilityRegistry
{
    private readonly Dictionary<int, AbilityRuntime> _abilitiesById = [];
    private readonly WordTrie<AbilityRuntime> _abilityWordTrie = new();

    private readonly EffectRegistry _effectRegistry;
    private readonly AbilityExecutionResolverRegistry _abilityExecutionResolverRegistry;

    public AbilityRegistry(EffectRegistry effectRegistry, AbilityExecutionResolverRegistry abilityExecutionResolverRegistry)
    {
        _effectRegistry = effectRegistry;
        _abilityExecutionResolverRegistry = abilityExecutionResolverRegistry;
    }

    public void RegisterAbilities(IEnumerable<AbilityDefinition> abilities)
    {
        foreach (var ability in abilities)
        {
            var abilityRuntime = AbilityRuntimeFactory.Create(_effectRegistry, _abilityExecutionResolverRegistry, ability);
            _abilitiesById.Add(ability.Id, abilityRuntime);
            _abilityWordTrie.Insert(ability.Name.ToLowerInvariant(), abilityRuntime);
        }
    }

    public bool TryGetValue(int abilityId, out AbilityRuntime? abilityRuntime)
        => _abilitiesById.TryGetValue(abilityId, out abilityRuntime);

    public bool StartsWith(string key, out AbilityRuntime? value)
        => _abilityWordTrie.StartsWith(key.ToLowerInvariant(), out value);
}
