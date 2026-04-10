namespace MysteryMud.GameData.Enums;

public struct AbilityTargeting
{
    public AbilityTargetKind Kind;              // Character / Item / Any
    public AbilityTargetScope Scope;            // Single / Room / Chain
    public AbilityTargetKindMask Allowed;       // Fine filtering

    public AbilityDefaultTargetRule Default;    // Self / Current opponent / None
    public bool Optional;

    // Multi-target
    public int MaxTargets;                      // 1 = Single
    public AbilityTargetSelection Selection;    // Random, LowestHealth

    // Filters
    public List<AbilityTargetFilterId> Filters;
}
