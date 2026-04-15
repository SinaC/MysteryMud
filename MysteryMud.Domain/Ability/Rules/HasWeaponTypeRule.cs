using Arch.Core;
using MysteryMud.Domain.Extensions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class HasWeaponTypeRule : AbilityValidationRule
{
    private readonly WeaponKind _required;

    public HasWeaponTypeRule(AbilityValidationRuleCondition condition, AbilityValidationFailBehaviour failBehaviour, string failMessageKey, WeaponKind required)
        : base(condition, failBehaviour, failMessageKey)
    {
        _required = required;
    }

    public override AbilityValidationResult Validate(Entity target)
    {
        if (!target.TryGetMainHandWeapon(out var _, out var weapon))
            return Fail();
        if (weapon.Kind != _required)
            return Fail();
        return Success();
    }
}
