using MysteryMud.Core.Trie;

namespace MysteryMud.Domain.Ability;

public class AbilityRegistry
{
    private readonly Dictionary<int, AbilityRuntime> _abilitiesById = [];
    private readonly WordTrie<AbilityRuntime> _abilityWordTrie = new();

    public void RegisterAbilities(IEnumerable<AbilityRuntime> abilities)
    {
        foreach (var ability in abilities)
        {
            _abilitiesById.Add(ability.Id, ability);
            _abilityWordTrie.Insert(ability.Name.ToLowerInvariant(), ability);
        }
    }

    public bool TryGetValue(int abilityId, out AbilityRuntime? abilityRuntime)
        => _abilitiesById.TryGetValue(abilityId, out abilityRuntime);

    public bool StartsWith(string key, out AbilityRuntime? value)
        => _abilityWordTrie.StartsWith(key.ToLowerInvariant(), out value);
}
