namespace MysteryMud.Infrastructure.Persistence.Dto;

public record AbilityTargetingData
(
    string Kind,           // Character / Item / Any
    string Scope,          // Single / Room / Chain
    List<string> Allowed,  // Fine filtering

    string Default,        // Self / Current opponent / None
    bool Optional,

    // Multi-target
    int MaxTargets,        // 1 = Single
    string Selection,      // Random, LowestHealth

    // Filters
    List<string> Filters
);
