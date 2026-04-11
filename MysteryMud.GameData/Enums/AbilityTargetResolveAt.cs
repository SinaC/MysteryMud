namespace MysteryMud.GameData.Enums;

// When the target list is resolved relative to the cast timeline.
//
// Single-target abilities always use CastStart (target must exist when cast begins).
// AoE abilities default to CastCompletion (snapshot at execution time) but can be
// overridden to CastStart for "snapshot AoE" semantics.
public enum AbilityTargetResolveAt
{
    CastStart,
    CastCompletion,
}