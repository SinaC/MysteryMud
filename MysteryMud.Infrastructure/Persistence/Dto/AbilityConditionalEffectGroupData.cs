namespace MysteryMud.Infrastructure.Persistence.Dto;

internal record AbilityConditionalEffectGroupData
(
    string Condition, // None, IsCharacter, IsItem, IsNPC, IsPlayer
    List<string> Effects
);
