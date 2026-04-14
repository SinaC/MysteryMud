using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public class CharacterNotAffectedByRuleDefinition : AbilityRuleDefinition
{
    public required CharacterEffectTagId EffectTagId { get; init; }
}
