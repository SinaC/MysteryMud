using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public class AbilityConditionalEffectGroupDefinition
{
    public required TargetCondition Condition { get; init; }
    public required List<string> Effects { get; init; }
}
