using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public class CharacterAffectedByRuleDefinition : AbilityRuleDefinition
{
    public required CharacterEffectTagId EffectTagId { get; init; }
}
