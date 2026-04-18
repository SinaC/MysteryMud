using Arch.Core;

namespace MysteryMud.Domain.Ability.Rules;

public interface IAbilityValidationRule
{
    bool IsCandidateForValidation(Entity target);
    AbilityValidationResult Validate(Entity source, Entity target);
}
