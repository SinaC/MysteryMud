using Arch.Core;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class HasWeaponTypeRule : IAbilityValidationRule
{
    private readonly WeaponKind _required;
    private readonly AbilityValidationRuleFailActions _failActions;
    private readonly string _failKey;

    public HasWeaponTypeRule(WeaponKind required, AbilityValidationRuleFailActions failActions, string failKey)
    {
        _required = required;
        _failActions = failActions;
        _failKey = failKey;
    }

    public AbilityValidationResult Validate(Entity target, AbilityRuntime ability)
    {
        if (!CharacterHelpers.TryGetMainHandWeapon(target, out var _, out var weapon))
            return Fail();
        if (weapon.Kind != _required)
            return Fail();
        return new AbilityValidationResult
        {
            Success = true,
        };
    }

    private AbilityValidationResult Fail()
        => new()
        {
            Success = false,
            FailActions = _failActions,
            FailureMessageKey = _failKey
        };
}
