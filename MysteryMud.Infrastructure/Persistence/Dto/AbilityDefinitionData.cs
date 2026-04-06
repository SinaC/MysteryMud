namespace MysteryMud.Infrastructure.Persistence.Dto;

public record AbilityDefinitionData
(
    string Name,
    string Kind, // spell/skill/passive/weapon
    int CastTime, // 0 means instant cast
    int Cooldown,
    List<ResourceCostData> Costs,
    List<string> Effects
);
