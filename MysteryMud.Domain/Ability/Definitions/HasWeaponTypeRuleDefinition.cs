using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class HasWeaponTypeRuleDefinition : AbilityRuleDefinition
{
    public required WeaponKind Required;
}
