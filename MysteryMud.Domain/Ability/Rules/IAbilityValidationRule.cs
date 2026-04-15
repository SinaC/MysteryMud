using Arch.Core;

namespace MysteryMud.Domain.Ability.Rules;

public interface IAbilityValidationRule
{
    bool CanBeValidated(Entity target);
    AbilityValidationResult Validate(Entity target);
}
