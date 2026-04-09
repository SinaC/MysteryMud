using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class AffectedByRuleDefinition : AbilityRuleDefinition
{
    public required EffectTagId EffectTagId { get; init; }
}
