namespace MysteryMud.Infrastructure.Persistence.Dto;

public record AbilityTargetingData
(
    // (*) default
    string Requirement, // Mandatory(*) / Optional / None
    string Selection,   // Single(*) / AoE
    List<AbilityTargetingContextData> Contexts, // ordered list of target context
    string ResolveAt    // CastStart(*) / CastCompletion
);
