using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.Infrastructure.Persistence.Converters;
using MysteryMud.Infrastructure.Persistence.Dto;
using MysteryMud.Infrastructure.Persistence.Dto.Rules;
using MysteryMud.Infrastructure.Persistence.Parsers;
using System.Data;
using System.Text.Json;

namespace MysteryMud.Infrastructure.Persistence;

public partial class JsonAbilityLoader
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        Converters = { new AbilityValidationRuleDataConverter(), new ContextualizedMessageConverter() },
        PropertyNameCaseInsensitive = true,
    };

    public List<AbilityDefinition> Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Ability JSON file not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<List<AbilityDefinitionData>>(json, _serializerOptions) ?? [];

        var abilities = new List<AbilityDefinition>();
        foreach (var entry in data)
        {
            var kind = Enum.Parse<AbilityKind>(entry.Kind, ignoreCase: true);
            CommandDefinition? command = entry.Command == null
                ? null
                : JsonCommandLoader.Map(entry.Command);
            if (kind == AbilityKind.Skill && command is null)
                throw new Exception($"Skill ability {entry.Name} must declare a command");

            var costs = entry.Costs?.Select(MapResourceCost).ToList() ?? [];
            var outcomeResolver = MapOutcomeResolver(entry.OutcomeResolver);
            var sourceValidationRules = entry.ValidationRules?.Source?.Select(x => MapRule(entry, x)).ToList();
            var targetValidationRules = entry.ValidationRules?.Target?.Select(x => MapRule(entry, x)).ToList();
            var conditionalEffectGroups = MapConditionalEffectGroups(entry);

            var ability = new AbilityDefinition
            {
                Id = entry.Name.ComputeUniqueId(),
                Name = entry.Name,
                Kind = kind,
                CastTime = entry.CastTime,
                Cooldown = entry.Cooldown,
                Costs = costs ?? [],
                Command = command,
                Targeting = MapTargeting(entry.Targeting),
                OutcomeResolver = outcomeResolver,
                Messages = entry.Messages?.ToDictionary(x => x.Key, x => MapContextualizedMessage(x.Value)) ?? [],
                SourceValidationRules = sourceValidationRules ?? [],
                TargetValidationRules = targetValidationRules ?? [],
                ConditionalEffectGroups = conditionalEffectGroups ?? [],
                FailureEffects = entry.FailureEffects ?? []
            };
            abilities.Add(ability);
        }
        return abilities;
    }

    private ContextualizedMessage MapContextualizedMessage(ContextualizedMessageData data)
    => new()
    {
        ToActor = data.ToActor,
        ToRoom = data.ToRoom,
        ToTarget = data.ToTarget,
    };

    private List<AbilityConditionalEffectGroupDefinition> MapConditionalEffectGroups(AbilityDefinitionData data)
    {
        if ((data.Effects == null || data.Effects.Count == 0) && (data.ConditionalEffects == null || data.ConditionalEffects.Count == 0))
            throw new Exception($"No effects nor condition effects found on ability {data.Name}");
        if (data.Effects != null && data.Effects.Count > 0) // if effects is found, consider them as a group without condition
            return [new AbilityConditionalEffectGroupDefinition { Condition = TargetCondition.None, Effects = data.Effects }];
        return [.. data.ConditionalEffects.Select(MapConditionalEffectGroup)];
    }

    private AbilityConditionalEffectGroupDefinition MapConditionalEffectGroup(AbilityConditionalEffectGroupData data)
        => new() { Condition = EnumParser.Parse(data.Condition, TargetCondition.None), Effects = data.Effects };

    private AbilityOutcomeResolverDefinition? MapOutcomeResolver(AbilityOutcomeResolverData data)
        => data == null
            ? null
            : new()
            {
                ResolverName = data.Name,
                Hook = EnumParser.Parse(data.Hook, AbilityOutcomeHook.Execution)
            };

    private ResourceCost MapResourceCost(ResourceCostData data)
        => new()
        {
            Kind = Enum.Parse<ResourceKind>(data.Kind, ignoreCase: true),
            Amount = data.Amount,
        };

    private AbilityTargetingDefinition MapTargeting(AbilityTargetingData data)
        => data == null
        ? new () // if not specified: mandatory/single/room/character
        : new()
        {
            Requirement = EnumParser.Parse(data.Requirement, AbilityTargetRequirement.Mandatory),
            Selection = EnumParser.Parse(data.Selection, AbilityTargetSelection.Single),
            Contexts = MapTargetingContexts(data),
            ResolveAt = EnumParser.Parse(data.ResolveAt, AbilityTargetResolveAt.CastStart),
        };

    private List<AbilityTargetingContextDefinition> MapTargetingContexts(AbilityTargetingData data)
    {
        if (data.Scope != null && data.Filter != null)
            return [new AbilityTargetingContextDefinition
            {
                Filter = FlagsEnumParser.Parse(data.Filter, AbilityTargetFilter.Character),
                Scope = EnumParser.Parse(data.Scope, AbilityTargetScope.Room)
            }];
        return data.Contexts?.Select(MapTargetingContext)?.ToList()
            ?? [new()]; // default: one context room/character
    }

    private AbilityTargetingContextDefinition MapTargetingContext(AbilityTargetingContextData data)
        => new()
        {
            Scope = EnumParser.Parse(data.Scope, AbilityTargetScope.Room),
            Filter = FlagsEnumParser.Parse(data.Filter, AbilityTargetFilter.Character),
        };

    private AbilityRuleDefinition MapRule(AbilityDefinitionData abilityDefinitionData, AbilityValidationRuleData data)
        => data switch
        {
            AffectedByRuleData rule => MapAffectedByRule(abilityDefinitionData, rule),
            NotAffectedByRuleData rule => MapNotAffectedByRule(abilityDefinitionData, rule),
            HasWeaponTypeRuleData rule => new HasWeaponTypeRuleDefinition { Condition = EnumParser.Parse(rule.Condition, TargetCondition.IsCharacter), FailBehaviour = EnumParser.Parse(rule.OnFail, AbilityValidationFailBehaviour.Abort), FailMessageKey = rule.MessageKey, Required = Enum.Parse<WeaponKind>(rule.Required) },
            NotFightingRuleData rule => new NotFightingRuleDefinition { Condition = EnumParser.Parse(rule.Condition, TargetCondition.IsCharacter), FailBehaviour = EnumParser.Parse(rule.OnFail, AbilityValidationFailBehaviour.Abort), FailMessageKey = rule.MessageKey },
            SavesSpellRuleData rule => new SavesSpellRuleDefinition { Condition = TargetCondition.IsCharacter, FailBehaviour = EnumParser.Parse(rule.OnFail, AbilityValidationFailBehaviour.Abort), FailMessageKey = rule.MessageKey, DamageKind = EnumParser.Parse(rule.DamageKind, DamageKind.None) },
            _ => throw new NotSupportedException($"Ability '{abilityDefinitionData.Name}' contains an unknown validation rule type: {data.GetType()}")
        };

    private AbilityRuleDefinition MapAffectedByRule(AbilityDefinitionData abilityDefinitionData, AffectedByRuleData rule)
    {
        if (rule.TagKind is null || rule.TagKind == "Character")
            return new CharacterAffectedByRuleDefinition { Condition = EnumParser.Parse(rule.Condition, TargetCondition.IsCharacter), FailBehaviour = EnumParser.Parse(rule.OnFail, AbilityValidationFailBehaviour.Abort), FailMessageKey = rule.MessageKey, EffectTagId = Enum.Parse<CharacterEffectTagId>(rule.Tag) };
        else if (rule.TagKind == "Item")
            return new ItemAffectedByRuleDefinition { Condition = EnumParser.Parse(rule.Condition, TargetCondition.IsItem), FailBehaviour = EnumParser.Parse(rule.OnFail, AbilityValidationFailBehaviour.Abort), FailMessageKey = rule.MessageKey, EffectTagId = Enum.Parse<ItemEffectTagId>(rule.Tag) };
        throw new NotSupportedException($"Ability '{abilityDefinitionData.Name}' contains an unknown rule tag kind: {rule.TagKind} on AffectedBy");
    }
    private AbilityRuleDefinition MapNotAffectedByRule(AbilityDefinitionData abilityDefinitionData, NotAffectedByRuleData rule)
    {
        if (rule.TagKind is null || rule.TagKind == "Character")
            return new CharacterNotAffectedByRuleDefinition { Condition = EnumParser.Parse(rule.Condition, TargetCondition.IsCharacter), FailBehaviour = EnumParser.Parse(rule.OnFail, AbilityValidationFailBehaviour.Abort), FailMessageKey = rule.MessageKey, EffectTagId = Enum.Parse<CharacterEffectTagId>(rule.Tag) };
        else if (rule.TagKind == "Item")
            return new ItemNotAffectedByRuleDefinition { Condition = EnumParser.Parse(rule.Condition, TargetCondition.IsItem), FailBehaviour = EnumParser.Parse(rule.OnFail, AbilityValidationFailBehaviour.Abort), FailMessageKey = rule.MessageKey, EffectTagId = Enum.Parse<ItemEffectTagId>(rule.Tag) };
        throw new NotSupportedException($"Ability '{abilityDefinitionData.Name}' contains an unknown rule tag kind: {rule.TagKind} on NotAffectedBy");
    }
}
