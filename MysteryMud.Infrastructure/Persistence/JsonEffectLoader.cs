using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Combat.Effect.Definitions;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using MysteryMud.Infrastructure.Persistence.Dto.Actions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MysteryMud.Infrastructure.Persistence;

public class JsonEffectLoader
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        Converters = { new EffectActionDataConverter() },
        PropertyNameCaseInsensitive = true
    };
    private static readonly EffectFormulaCompiler _formulaCompiler = new();

    public List<EffectDefinition> Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Effect JSON file not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<List<EffectDefinitionData>>(json, _serializerOptions) ?? [];

        var effects = new List<EffectDefinition>();
        foreach (var entry in data)
        {
            var effect = MapEffect(entry);
            effects.Add(effect);
        }

        return effects;
    }

    private EffectDefinition MapEffect(EffectDefinitionData data)
    {
        // map actions
        var actions = data.Actions.Select(MapAction).ToList();

        return new EffectDefinition
        {
            Id = data.Name.ComputeUniqueId(),
            Name = data.Name,

            DurationFunc = data.DurationFormula == null
                ? null
                : _formulaCompiler.Compile(data.DurationFormula),
            Tag = data.Tag == null
                ? EffectTagId.None
                : Enum.Parse<EffectTagId>(data.Tag, ignoreCase: true),
            Stacking = data.Stacking == null
                ? StackingRule.None
                : Enum.Parse<StackingRule>(data.Stacking, ignoreCase: true),
            MaxStacks = data.MaxStacks,
            TickOnApply = data.TickOnApply,
            TickRate = data.TickRate,

            WearOffMessage = data.WearOffMessage,
            ApplyMessage = data.ApplyMessage,

            Actions = actions
        };
    }

    private EffectActionDefinition MapAction(EffectActionData action)
    {
        var trigger = Enum.Parse<TriggerType>(action.Trigger, ignoreCase: true);
        switch (action)
        {
            case StatModifierData data:
                {
                    var stat = Enum.Parse<StatKind>(data.Stat, ignoreCase: true);
                    var modifier = Enum.Parse<ModifierKind>(data.Mode, ignoreCase: true);
                    var valueFunc = _formulaCompiler.Compile(data.ValueFormula);
                    return new StatModifierActionDefinition
                    {
                        Trigger = trigger,
                        Stat = stat,
                        Modifier = modifier,
                        ValueFunc = valueFunc
                    };
                }

            case PeriodicHealData data:
                {
                    var amountFunc = _formulaCompiler.Compile(data.HealFormula);
                    return new PeriodicHealActionDefinition
                    {
                        Trigger = trigger,
                        AmountFunc = amountFunc
                    };
                }

            case InstantHealData data:
                {
                    var amountFunc = _formulaCompiler.Compile(data.HealFormula);
                    return new InstantHealActionDefinition
                    {
                        Trigger = trigger,
                        AmountFunc = amountFunc
                    };
                }

            case PeriodicDamageData data:
                {
                    var amountFunc = _formulaCompiler.Compile(data.DamageFormula);
                    var dmgKind = Enum.Parse<DamageKind>(data.DamageKind, ignoreCase: true);
                    return new PeriodicDamageActionDefinition
                    {
                        Trigger = trigger,
                        AmountFunc = amountFunc,
                        Kind = dmgKind
                    };
                }

            case InstantDamageData data:
                {
                    var amountFunc = _formulaCompiler.Compile(data.DamageFormula);
                    var dmgKind = Enum.Parse<DamageKind>(data.DamageKind, ignoreCase: true);
                    return new InstantDamageActionDefinition
                    {
                        Trigger = trigger,
                        AmountFunc = amountFunc,
                        Kind = dmgKind
                    };
                }
            default:
                throw new NotSupportedException($"Unknown action type: {action.GetType()}");
        }
    }

    private class EffectActionDataConverter : JsonConverter<EffectActionData>
    {
        public override EffectActionData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var root = jsonDoc.RootElement;

            var type = root.GetProperty("Type").GetString();

            return type switch
            {
                "StatModifier" => JsonSerializer.Deserialize<StatModifierData>(root.GetRawText(), options),
                "PeriodicHeal" => JsonSerializer.Deserialize<PeriodicHealData>(root.GetRawText(), options),
                "PeriodicDamage" => JsonSerializer.Deserialize<PeriodicDamageData>(root.GetRawText(), options),
                "InstantDamage" => JsonSerializer.Deserialize<InstantDamageData>(root.GetRawText(), options),
                "InstantHeal" => JsonSerializer.Deserialize<InstantHealData>(root.GetRawText(), options),
                _ => throw new NotSupportedException($"Unknown action type: {type}")
            };
        }

        public override void Write(Utf8JsonWriter writer, EffectActionData value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
