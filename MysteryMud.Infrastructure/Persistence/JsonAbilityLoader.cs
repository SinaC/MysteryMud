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
            if (entry.Effects == null || entry.Effects.Count == 0)
                throw new Exception($"No effect found on ability {entry.Name}");

            var kind = Enum.Parse<AbilityKind>(entry.Kind, ignoreCase: true);
            CommandDefinition? command = entry.Command == null
                ? null
                : JsonCommandLoader.Map(entry.Command);
            if (kind == AbilityKind.Skill && command is null)
                throw new Exception($"Skill ability {entry.Name} must declare a command");

            var costs = entry.Costs?.Select(MapResourceCost).ToList() ?? [];
            var validationRules = entry.ValidationRules?.Select(MapRule).ToList();

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
                Executor = entry.Executor,
                Messages = entry.Messages ?? [],
                ValidationRules = validationRules ?? [],
                Effects = entry.Effects ?? [],
                FailureEffects = entry.FailureEffects ?? []
            };
            abilities.Add(ability);
        }
        return abilities;
    }

    private ResourceCost MapResourceCost(ResourceCostData data)
        => new()
        {
            Kind = Enum.Parse<ResourceKind>(data.Kind, ignoreCase: true),
            Amount = data.Amount,
        };

    private AbilityTargeting MapTargeting(AbilityTargetingData data)
        => data == null
        ? new AbilityTargeting // if not specified, single character
        {
            Kind = AbilityTargetKind.None,
            Scope = AbilityTargetScope.Single,
            Allowed = AbilityTargetKindMask.AnyCharacter,

            Default = AbilityDefaultTargetRule.None,
            Optional = false,

            MaxTargets = 0, // no max target
            Selection = AbilityTargetSelection.None,

            Filters = [] // no filter
        }
        : new()
        {
            Kind = Enum.Parse<AbilityTargetKind>(data.Kind),
            Scope = Enum.Parse<AbilityTargetScope>(data.Scope),
            Allowed = data.Allowed == null || data.Allowed.Count == 0
                ? AbilityTargetKindMask.Any
                : data.Allowed.Aggregate(AbilityTargetKindMask.None, (r, f) => r | Enum.Parse<AbilityTargetKindMask>(f)),

            Default = data.Default == null
                ? AbilityDefaultTargetRule.None
                : Enum.Parse<AbilityDefaultTargetRule>(data.Default),
            Optional = data.Optional,

            MaxTargets = data.MaxTargets,
            Selection = data.Selection == null
                ? AbilityTargetSelection.None
                : Enum.Parse<AbilityTargetSelection>(data.Selection),

            Filters = data.Filters?.Select(Enum.Parse<AbilityTargetFilterId>)?.ToList() ?? []
        };

    private AbilityRuleDefinition MapRule(AbilityValidationRuleData data)
        => data switch
        {
            AffectedByRuleData rule => new AffectedByRuleDefinition { Scope = Enum.Parse<AbilityValidationRuleScope>(rule.Scope), FailActions = MapFailActions(data), FailMessageKey = rule.Fail, EffectTagId = Enum.Parse<EffectTagId>(rule.Tag) },
            HasWeaponTypeRuleData rule => new HasWeaponTypeRuleDefinition { Scope = Enum.Parse<AbilityValidationRuleScope>(rule.Scope), FailActions = MapFailActions(data), FailMessageKey = rule.Fail, Required = Enum.Parse<WeaponKind>(rule.Required) },
            NotAffectedByRuleData rule => new NotAffectedByRuleDefinition { Scope = Enum.Parse<AbilityValidationRuleScope>(rule.Scope), FailActions = MapFailActions(data), FailMessageKey = rule.Fail, EffectTagId = Enum.Parse<EffectTagId>(rule.Tag) },
            NotFightingRuleData rule => new NotFightingRuleDefinition { Scope = Enum.Parse<AbilityValidationRuleScope>(rule.Scope), FailActions = MapFailActions(data), FailMessageKey = rule.Fail },
            _ => throw new NotSupportedException($"Unknown rule type: {data.GetType()}")
        };

    private AbilityValidationRuleFailActions MapFailActions(AbilityValidationRuleData data)
    {
        var failActions = AbilityValidationRuleFailActions.None;
        if (data.OnFail == "SkipTarget")
            failActions |= AbilityValidationRuleFailActions.SkipTarget;
        if (data.Fail != null)
            failActions |= AbilityValidationRuleFailActions.DisplayMessage;
        return failActions;
    }

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
