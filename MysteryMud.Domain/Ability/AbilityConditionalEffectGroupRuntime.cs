using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability;

public class AbilityConditionalEffectGroupRuntime
{
    public required TargetCondition Condition { get; init; }
    public required List<int> EffectIds { get; init; }
}
