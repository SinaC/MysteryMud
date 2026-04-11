using Arch.Core;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class HasWeaponTypeRule : IAbilityValidationRule
{
    private readonly WeaponKind _required;
    private readonly AbilityValidationFailBehaviour _failBehaviour;
    private readonly string _failMessageKey;

    public HasWeaponTypeRule(WeaponKind required, AbilityValidationFailBehaviour failBehaviour, string failMessageKey)
    {
        _required = required;
        _failBehaviour = failBehaviour;
        _failMessageKey = failMessageKey;
    }

    public AbilityValidationResult Validate(Entity target)
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
            FailBehaviour = _failBehaviour,
            FailMessageKey = _failMessageKey
        };
}
