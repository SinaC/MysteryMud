using MysteryMud.GameData.Definitions;

namespace MysteryMud.Domain.Components.Effects;

public struct EffectValuesSnapshot
{
    public int SourceLevel;
    public StatValues SourceStats;

    public int TargetLevel;
    public StatValues TargetStats;
}
