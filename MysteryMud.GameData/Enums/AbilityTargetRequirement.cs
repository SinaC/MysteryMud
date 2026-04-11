namespace MysteryMud.GameData.Enums;

// Whether a player-supplied target argument is required.
public enum AbilityTargetRequirement
{
    // A valid target must be supplied or inferable; ability is aborted if none found.
    Mandatory,

    // Target is optional; falls back to current opponent, then self if none.
    Optional,

    // No target accepted; ability always applies to the caster.
    None,
}
