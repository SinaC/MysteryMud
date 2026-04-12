using MysteryMud.Infrastructure.Persistence.Dto.Rules;

namespace MysteryMud.Infrastructure.Persistence.Dto;

internal record AbilityDefinitionData
(
    string Name,
    string Kind, // spell/skill/passive/weapon
    int CastTime, // 0 means instant cast
    int Cooldown,
    List<ResourceCostData> Costs,
    CommandDefinitionData Command,
    AbilityTargetingData Targeting,
    AbilityOutcomeResolverData OutcomeResolver,
    Dictionary<string, string> Messages,
    AbilityValidationRulesData ValidationRules,
    List<AbilityConditionalEffectGroupData> ConditionalEffects,
    List<string> Effects, // will be converted to ConditionalEffectGroups with Condition=None
    List<string> FailureEffects
);
