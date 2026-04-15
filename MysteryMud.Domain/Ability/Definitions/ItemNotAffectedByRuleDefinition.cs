using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public class ItemNotAffectedByRuleDefinition : AbilityRuleDefinition
{
    public required ItemEffectTagId EffectTagId { get; init; }
}
