using MysteryMud.Infrastructure.Persistence.Dto.Rules;

namespace MysteryMud.Infrastructure.Persistence.Dto;

public record AbilityDefinitionData
(
    string Name,
    string Kind, // spell/skill/passive/weapon
    int CastTime, // 0 means instant cast
    int Cooldown,
    List<ResourceCostData> Costs,
    CommandDefinitionData Command,
    string Executor,
    Dictionary<string, string> Messages,
    List<AbilityValidationRuleData> ValidationRules,
    List<string> Effects,
    List<string> FailureEffects
);
