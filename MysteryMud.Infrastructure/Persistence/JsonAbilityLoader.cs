using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Ability.Definitions;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.Infrastructure.Persistence.Dto;
using MysteryMud.Infrastructure.Persistence.Dto.Rules;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MysteryMud.Infrastructure.Persistence;

public class JsonAbilityLoader
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        Converters = { new AbilityValidationRuleDataConverter() },
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
            var sourceValidationRules = entry.ValidationRules?.Source?.Select(MapRule).ToList();
            var targetValidationRules = entry.ValidationRules?.Target?.Select(MapRule).ToList();
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
                Messages = entry.Messages ?? [],
                SourceValidationRules = sourceValidationRules ?? [],
                TargetValidationRules = targetValidationRules ?? [],
                ConditionalEffectGroups = conditionalEffectGroups ?? [],
                FailureEffects = entry.FailureEffects ?? []
            };
            abilities.Add(ability);
        }
        return abilities;
    }

    private List<AbilityConditionalEffectGroupDefinition> MapConditionalEffectGroups(AbilityDefinitionData data)
    {
        if ((data.Effects == null || data.Effects.Count == 0) && (data.ConditionalEffects == null || data.ConditionalEffects.Count == 0))
            throw new Exception($"No effects nor condition effects found on ability {data.Name}");
        if (data.Effects != null && data.Effects.Count > 0) // if effects is found, consider them as a group without condition
            return [new AbilityConditionalEffectGroupDefinition { Condition = AbilityEffectCondition.None, Effects = data.Effects }];
        return [.. data.ConditionalEffects.Select(MapConditionalEffectGroup)];
    }

    private AbilityConditionalEffectGroupDefinition MapConditionalEffectGroup(AbilityConditionalEffectGroupData data)
        => new() { Condition = EnumParser.Parse(data.Condition, AbilityEffectCondition.None), Effects = data.Effects };

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

    private AbilityRuleDefinition MapRule(AbilityValidationRuleData data)
        => data switch
        {
            AffectedByRuleData rule => new AffectedByRuleDefinition { FailBehaviour = EnumParser.Parse(rule.OnFail, AbilityValidationFailBehaviour.Abort), FailMessageKey = rule.MessageKey, EffectTagId = Enum.Parse<EffectTagId>(rule.Tag) },
            HasWeaponTypeRuleData rule => new HasWeaponTypeRuleDefinition { FailBehaviour = EnumParser.Parse(rule.OnFail, AbilityValidationFailBehaviour.Abort), FailMessageKey = rule.MessageKey, Required = Enum.Parse<WeaponKind>(rule.Required) },
            NotAffectedByRuleData rule => new NotAffectedByRuleDefinition { FailBehaviour = EnumParser.Parse(rule.OnFail, AbilityValidationFailBehaviour.Abort), FailMessageKey = rule.MessageKey, EffectTagId = Enum.Parse<EffectTagId>(rule.Tag) },
            NotFightingRuleData rule => new NotFightingRuleDefinition { FailBehaviour = EnumParser.Parse(rule.OnFail, AbilityValidationFailBehaviour.Abort), FailMessageKey = rule.MessageKey },
            _ => throw new NotSupportedException($"Unknown rule type: {data.GetType()}")
        };


    private class AbilityValidationRuleDataConverter : JsonConverter<AbilityValidationRuleData>
    {
        public override AbilityValidationRuleData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var root = jsonDoc.RootElement;

            var type = root.GetProperty("Type").GetString();

            return type switch
            {
                "AffectedBy" => JsonSerializer.Deserialize<AffectedByRuleData>(root.GetRawText(), options),
                "HasWeaponType" => JsonSerializer.Deserialize<HasWeaponTypeRuleData>(root.GetRawText(), options),
                "NotAffectedBy" => JsonSerializer.Deserialize<NotAffectedByRuleData>(root.GetRawText(), options),
                "NotFighting" => JsonSerializer.Deserialize<NotFightingRuleData>(root.GetRawText(), options),
                _ => throw new NotSupportedException($"Unknown rule type: {type}")
            };
        }

        public override void Write(Utf8JsonWriter writer, AbilityValidationRuleData value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
