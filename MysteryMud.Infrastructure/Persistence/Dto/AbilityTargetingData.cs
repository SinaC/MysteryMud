namespace MysteryMud.Infrastructure.Persistence.Dto;

public record AbilityTargetingData
(
    // (*) default
    string Requirement, // Mandatory(*) / Optional / None
    string Selection,   // Single(*) / AoE
    string Scope,       // Room(*) / World / Inventory / Self
    string Filter,      // None, Player, NPC, Item, Character(*), Any
    string ResolveAt    // CastStart(*) / CastCompletion
);
