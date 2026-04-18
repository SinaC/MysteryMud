using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.Domain.Ability.Rules;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability;

public class AbilityRuntime
{
    public int Id; // generated
    public string Name = default!;
    public AbilityKind Kind;
    public int CastTime = 0; // 0 means instant cast
    public int Cooldown = 0;
    public List<ResourceCost> Costs = [];
    public AbilityTargetingDefinition Targeting = new();
    public AbilityOutcomeResolverRuntime? OutcomeResolver;
    public List<AbilityConditionalEffectGroupRuntime> ConditionalEffectGroups = [];
    public List<int> FailureEffectIds = [];
    public Dictionary<string, ContextualizedMessage> Messages = [];
    public List<IAbilityValidationRule> SourceValidationRules = [];
    public List<IAbilityValidationRule> TargetValidationRules = [];
}
