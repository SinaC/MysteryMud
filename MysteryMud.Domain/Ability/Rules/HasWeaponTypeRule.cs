using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Ability.Rules;

public class HasWeaponTypeRule : AbilityValidationRule
{
    private readonly WeaponKind _required;

    public HasWeaponTypeRule(TargetCondition condition, AbilityValidationFailBehaviour failBehaviour, string failMessageKey, WeaponKind required)
        : base(condition, failBehaviour, failMessageKey)
    {
        _required = required;
    }

    public override AbilityValidationResult Validate(World world, EntityId _, EntityId target)
    {
        if (!CharacterHelpers.TryGetMainHandWeapon(world, target, out var _, out var weapon))
            return Fail();
        if (weapon.Kind != _required)
            return Fail();
        return Success();
    }
}
