using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public class ItemAffectedByRuleDefinition : AbilityRuleDefinition
{
    public required ItemEffectTagId EffectTagId { get; init; }
}
