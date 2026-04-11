using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public class NotAffectedByRuleDefinition : AbilityRuleDefinition
{
    public required EffectTagId EffectTagId { get; init; }
}
