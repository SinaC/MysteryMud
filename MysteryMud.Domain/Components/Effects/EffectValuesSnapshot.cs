using MysteryMud.GameData.Definitions;

namespace MysteryMud.Domain.Components.Effects;

public struct EffectValuesSnapshot
{
    public int SourceLevel;
    public CharacterStatValues SourceStats;

    public int TargetLevel;
    public CharacterStatValues TargetStats;
}
