using Arch.Core;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class HasWeaponTypeRule : IAbilityValidationRule
{
    private readonly WeaponKind _required;
    private readonly string _failKey;

    public HasWeaponTypeRule(WeaponKind required, string failKey)
    {
        _required = required;
        _failKey = failKey;
    }

    public AbilityValidationResult Validate(Entity caster, List<Entity> targets, AbilityRuntime ability)
    {
        if (!CharacterHelpers.TryGetMainHandWeapon(caster, out var _, out var weapon))
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
            FailureMessageKey = _failKey
        };
}
