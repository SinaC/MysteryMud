using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public class SavesSpellRuleDefinition : AbilityRuleDefinition
{
    public required DamageKind DamageKind { get; init; }
}
