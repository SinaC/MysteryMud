namespace MysteryMud.Domain.Ability.Services;

public enum TargetResolutionStatus
{
    // One or more valid targets found (or AoE with zero targets — still valid).
    Ok,

    // Mandatory/Optional single-target: no valid target could be resolved.
    NoTarget,

    // A target argument was supplied but matched no entity in the search scope.
    TargetNotFound,

    // An explicit target was supplied but failed a TargetFilter check (wrong entity type).
    InvalidTarget,
}
