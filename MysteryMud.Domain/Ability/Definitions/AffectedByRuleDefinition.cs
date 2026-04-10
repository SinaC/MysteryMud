using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public class AffectedByRuleDefinition : AbilityRuleDefinition
{
    public required EffectTagId EffectTagId { get; init; }
}
