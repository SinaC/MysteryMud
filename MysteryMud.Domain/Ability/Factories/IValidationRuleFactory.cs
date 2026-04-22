using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Ability.Rules;

namespace MysteryMud.Domain.Ability.Factories
{
    public interface IValidationRuleFactory
    {
        IAbilityValidationRule Create(AbilityRuleDefinition def);
    }
}