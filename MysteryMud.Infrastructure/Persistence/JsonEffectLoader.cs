using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Action.Effect.Definitions;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.Infrastructure.Persistence.Converters;
using MysteryMud.Infrastructure.Persistence.Dto;
using MysteryMud.Infrastructure.Persistence.Dto.Actions;
using MysteryMud.Infrastructure.Persistence.Parsers;
using System.Text.Json;

namespace MysteryMud.Infrastructure.Persistence;

public class JsonEffectLoader
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        Converters = { new EffectActionDataConverter(), new ContextualizedMessageConverter() },
        PropertyNameCaseInsensitive = true
    };

    private readonly EffectFormulaCompiler _formulaCompiler;

    public JsonEffectLoader(EffectFormulaCompiler formulaCompiler)
    {
        _formulaCompiler = formulaCompiler;
    }

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

        var isHarmful = MapIsHarmful(data);
        var tag = MapTag(data, actions);

        return new EffectDefinition
        {
            Id = data.Name.ComputeUniqueId(),
            Name = data.Name,

            IsHarmful = isHarmful,
            DurationCompiledFormula = data.DurationFormula == null
                ? null
                : _formulaCompiler.Compile(data.DurationFormula),
            Tag = tag,
            Stacking = EnumParser.Parse(data.Stacking, StackingRule.None),
            MaxStacks = data.MaxStacks,
            TickOnApply = data.TickOnApply,
            TickRate = data.TickRate,

            WearOffMessage = MapContextualizedMessage(data.WearOffMessage),
            ApplyMessage = MapContextualizedMessage(data.ApplyMessage),

            Actions = actions
        };
    }

    private ContextualizedMessage? MapContextualizedMessage(ContextualizedMessageData data)
        => data == null
        ? null
        : new()
        {
            ToActor = data.ToActor,
            ToRoom = data.ToRoom,
            ToTarget = data.ToTarget,
        };

    private bool MapIsHarmful(EffectDefinitionData data)
    {
        if (data.IsHarmful is not null)
            return data.IsHarmful.Value;
        return data.Actions.Any(x => x.IsDebuff == true || (x is InstantDamageData or PeriodicDamageData));
    }

    private EffectTagRef MapTag(EffectDefinitionData data, IEnumerable<EffectActionDefinition> actions)
    {
        if (data.Tag == null)
            return default;

        // 1. Explicit override always wins
        if (data.TagKind != null)
        {
            return data.TagKind switch
            {
                "Character" => EffectTagRef.ForCharacter(Enum.Parse<CharacterEffectTagId>(data.Tag)),
                "Item" => EffectTagRef.ForItem(Enum.Parse<ItemEffectTagId>(data.Tag)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        // 2. Infer from actions
        var inferredKind = actions
            .Select(EffectActionRegistry.GetAllowedTargets)
            .Aggregate((a, b) => a & b);

        // 3. Unambiguous inference
        if (inferredKind == EffectTargetKind.Character)
            return EffectTagRef.ForCharacter(Enum.Parse<CharacterEffectTagId>(data.Tag));
        if (inferredKind == EffectTargetKind.Item)
            return EffectTagRef.ForItem(Enum.Parse<ItemEffectTagId>(data.Tag));

        // 4. Still ambiguous — fail loudly
        throw new Exception(
            $"Effect '{data.Name}': tag '{data.Tag}' is ambiguous (actions support both Character and Item). " +
            $"Add \"TagKind\": \"Character\" or \"TagKind\": \"Item\" to resolve.");
    }

    private EffectActionDefinition MapAction(EffectActionData action)
    {
        var trigger = Enum.Parse<TriggerType>(action.Trigger, ignoreCase: true);
        switch (action)
        {
            case CharacterStatModifierData data:
                {
                    var stat = Enum.Parse<CharacterStatKind>(data.Stat, ignoreCase: true);
                    var modifier = Enum.Parse<ModifierKind>(data.Mode, ignoreCase: true);
                    var valueFunc = _formulaCompiler.Compile(data.ValueFormula);
                    return new CharacterStatModifierActionDefinition
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
                        "Move" => new MoveModifierActionDefinition
                        {
                            Trigger = trigger,
                            Modifier = modifier,
                            ValueCompiledFormula = valueFunc
                        },
                        "Mana" or "Energy" or "Rage" => new ResourceModifierActionDefinition
                        {
                            Resource = Enum.Parse<ResourceKind>(data.Resource, ignoreCase: true),
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
                        "Move" => new MoveRegenModifierActionDefinition
                        {
                            Trigger = trigger,
                            Modifier = modifier,
                            ValueCompiledFormula = valueFunc
                        },
                        "Mana" or "Energy" or "Rage" => new ResourceRegenModifierActionDefinition
                        {
                            Resource = Enum.Parse<ResourceKind>(data.Resource, ignoreCase: true),
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

            case InstantHealData data:
                {
                    var amountFunc = _formulaCompiler.Compile(data.HealFormula);
                    return new InstantHealActionDefinition
                    {
                        Trigger = trigger,
                        AmountCompiledFormula = amountFunc
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

            case InstantRestoreMoveData data:
                {
                    var amountFunc = _formulaCompiler.Compile(data.MoveFormula);
                    return new InstantRestoreMoveActionDefinition
                    {
                        Trigger = trigger,
                        AmountCompiledFormula = amountFunc
                    };
                }

            case InstantRestoreResourceData data:
                {
                    var amountFunc = _formulaCompiler.Compile(data.ValueFormula);
                    var resource = Enum.Parse<ResourceKind>(data.Resource);
                    return new InstantRestoreResourceActionDefinition
                    {
                        Resource = resource,
                        Trigger = trigger,
                        AmountCompiledFormula = amountFunc
                    };
                }

            case ApplyCharacterTagActionData data:
                {
                    var effectTagId = Enum.Parse<CharacterEffectTagId>(data.Tag);
                    return new ApplyCharacterTagActionDefinition
                    {
                        Trigger = trigger,
                        EffectTagId = effectTagId
                    };
                }

            case ApplyItemTagActionData data:
                {
                    var effectTagId = Enum.Parse<ItemEffectTagId>(data.Tag);
                    return new ApplyItemTagActionDefinition
                    {
                        Trigger = trigger,
                        EffectTagId = effectTagId
                    };
                }
            default:
                throw new NotSupportedException($"Unknown action type: {action.GetType()}");
        }
    }
}
