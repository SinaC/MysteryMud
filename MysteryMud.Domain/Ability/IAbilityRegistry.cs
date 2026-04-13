using MysteryMud.Domain.Ability.Definitions;

namespace MysteryMud.Domain.Ability;

public interface IAbilityRegistry
{
    void Register(AbilityDefinition ability);
    void Register(IEnumerable<AbilityDefinition> abilities);

    public bool TryGetRuntime(int abilityId, out AbilityRuntime? abilityRuntime);
    public bool StartsWith(string key, out AbilityRuntime? value);
}
