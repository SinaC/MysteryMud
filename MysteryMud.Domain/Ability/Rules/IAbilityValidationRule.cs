using Arch.Core;

namespace MysteryMud.Domain.Ability.Rules;

public interface IAbilityValidationRule
{
    AbilityValidationResult Validate(Entity target);
}
