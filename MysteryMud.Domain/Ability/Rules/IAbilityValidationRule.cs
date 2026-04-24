using TinyECS;

namespace MysteryMud.Domain.Ability.Rules;

public interface IAbilityValidationRule
{
    bool IsCandidateForValidation(World world, EntityId target);
    AbilityValidationResult Validate(World world, EntityId source, EntityId target);
}
