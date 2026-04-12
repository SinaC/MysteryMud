using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Definitions;

public class AbilityConditionalEffectGroupRuntime
{
    public required AbilityEffectCondition Condition { get; init; }
    public required List<int> EffectIds { get; init; }
}
