using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Definitions;

public class AbilityTargetingContextDefinition
{
    public AbilityTargetScope Scope { get; init; } = AbilityTargetScope.Room;
    public AbilityTargetFilter Filter { get; init; } = AbilityTargetFilter.Character;

}
