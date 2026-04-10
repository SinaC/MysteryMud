using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public class HasWeaponTypeRuleDefinition : AbilityRuleDefinition
{
    public required WeaponKind Required;
}
