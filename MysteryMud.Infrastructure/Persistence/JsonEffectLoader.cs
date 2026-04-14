using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Action.Effect.Definitions;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using MysteryMud.Infrastructure.Persistence.Dto;
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
        var actions = data.Actions?.Select(MapAction).ToList() ?? [];

        return new EffectDefinition
        {
            Id = data.Name.ComputeUniqueId(),
            Name = data.Name,

            DurationCompiledFormula = data.DurationFormula == null
                ? null
                : _formulaCompiler.Compile(data.DurationFormula),
            Tag = EnumParser.Parse(data.Tag, EffectTagId.None),
            Stacking = EnumParser.Parse(data.Stacking, StackingRule.None),
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
                        ValueCompiledFormula = valueFunc
                    };
                }

            case ResourceModifierData data:
                {
                    var modifier = Enum.Parse<ModifierKind>(data.Mode, ignoreCase: true);
                    var valueFunc = _formulaCompiler.Compile(data.ValueFormula);
                    return data.Resource switch
                    {
                        "Health" => new HealthModifierActionDefinition
                        {
                            Trigger = trigger,
                            Modifier = modifier,
                            ValueCompiledFormula = valueFunc
                        },
                        "Mana" => new ManaModifierActionDefinition
                        {
                            Trigger = trigger,
                            Modifier = modifier,
                            ValueCompiledFormula = valueFunc
                        },
                        "Energy" => new EnergyModifierActionDefinition
                        {
                            Trigger = trigger,
                            Modifier = modifier,
                            ValueCompiledFormula = valueFunc
                        },
                        "Rage" => new RageModifierActionDefinition
                        {
                            Trigger = trigger,
                            Modifier = modifier,
                            ValueCompiledFormula = valueFunc
                        },
                        _ => throw new NotSupportedException($"Unknown resource modifier type: {data.Resource}")
                    };
                }

            case RegenModifierData data:
                {
                    var modifier = Enum.Parse<ModifierKind>(data.Mode, ignoreCase: true);
                    var valueFunc = _formulaCompiler.Compile(data.ValueFormula);
                    return data.Resource switch
                    {
                        "Health" => new HealthRegenModifierActionDefinition
                        {
                            Trigger = trigger,
                            Modifier = modifier,
                            ValueCompiledFormula = valueFunc
                        },
                        "Mana" => new ManaRegenModifierActionDefinition
                        {
                            Trigger = trigger,
                            Modifier = modifier,
                            ValueCompiledFormula = valueFunc
                        },
                        "Energy" => new EnergyRegenModifierActionDefinition
                        {
                            Trigger = trigger,
                            Modifier = modifier,
                            ValueCompiledFormula = valueFunc
                        },
                        "Rage" => new RageRegenModifierActionDefinition
                        {
                            Trigger = trigger,
                            Modifier = modifier,
                            ValueCompiledFormula = valueFunc
                        },
                        _ => throw new NotSupportedException($"Unknown regen resource modifier type: {data.Resource}")
                    };
                }

            case PeriodicHealData data:
                {
                    var mode = EnumParser.Parse(data.Mode, EffectFormulaEvaluationMode.Snapshotted);
                    var amountFunc = _formulaCompiler.Compile(data.HealFormula, mode);
                    return new PeriodicHealActionDefinition
                    {
                        Trigger = trigger,
                        Mode = mode,
                        AmountCompiledFormula = amountFunc
                    };
                }

            case InstantHealData data:
                {
                    var amountFunc = _formulaCompiler.Compile(data.HealFormula);
                    return new InstantHealActionDefinition
                    {
                        Trigger = trigger,
                        AmountCompiledFormula = amountFunc
                    };
                }

            case PeriodicDamageData data:
                {
                    var mode = EnumParser.Parse(data.Mode, EffectFormulaEvaluationMode.Snapshotted);
                    var amountFunc = _formulaCompiler.Compile(data.DamageFormula, mode);
                    var dmgKind = Enum.Parse<DamageKind>(data.DamageKind, ignoreCase: true);
                    return new PeriodicDamageActionDefinition
                    {
                        Trigger = trigger,
                        Mode = mode,
                        AmountCompiledFormula = amountFunc,
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
                        AmountCompiledFormula = amountFunc,
                        Kind = dmgKind
                    };
                }

            case ApplyTagActionData data:
                {
                    var effectTagId = Enum.Parse<EffectTagId>(data.Tag);
                    // TODO: target
                    return new ApplyTagActionDefinition
                    {
                        Trigger = trigger,
                        EffectTagId = effectTagId
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
                "ResourceModifier" => JsonSerializer.Deserialize<ResourceModifierData>(root.GetRawText(), options),
                "RegenModifier" => JsonSerializer.Deserialize<RegenModifierData>(root.GetRawText(), options),
                "PeriodicHeal" => JsonSerializer.Deserialize<PeriodicHealData>(root.GetRawText(), options),
                "PeriodicDamage" => JsonSerializer.Deserialize<PeriodicDamageData>(root.GetRawText(), options),
                "InstantDamage" => JsonSerializer.Deserialize<InstantDamageData>(root.GetRawText(), options),
                "InstantHeal" => JsonSerializer.Deserialize<InstantHealData>(root.GetRawText(), options),
                "ApplyTag" => JsonSerializer.Deserialize<ApplyTagActionData>(root.GetRawText(), options),
                _ => throw new NotSupportedException($"Unknown action type: {type}")
            };
        }

        public override void Write(Utf8JsonWriter writer, EffectActionData value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
