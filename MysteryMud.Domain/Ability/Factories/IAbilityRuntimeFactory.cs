using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Action.Effect;

namespace MysteryMud.Domain.Ability.Factories;

public interface IAbilityRuntimeFactory
{
    AbilityRuntime Create(IEffectRegistry effectRegistry, IAbilityOutcomeResolverRegistry abilityOutcomeResolverRegistry, AbilityDefinition def);
}