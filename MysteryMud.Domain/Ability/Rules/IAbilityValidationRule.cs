using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public interface IAbilityValidationRule
{
    AbilityValidationResult Validate(Entity target, AbilityRuntime ability);
}
