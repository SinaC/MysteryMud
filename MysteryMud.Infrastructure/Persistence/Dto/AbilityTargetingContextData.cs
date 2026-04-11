namespace MysteryMud.Infrastructure.Persistence.Dto;

internal record AbilityTargetingContextData
(
    string Scope,       // Room(*) / World / Inventory / Self
    string Filter      // None, Player, NPC, Item, Character(*), Any
);
