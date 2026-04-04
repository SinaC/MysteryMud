using Arch.Core.Extensions;
using MysteryMud.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Damage;
using MysteryMud.Domain.Effect;
using MysteryMud.Domain.Heal;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.Infrastructure.Persistence.Dto.Effects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MysteryMud.Infrastructure.Persistence;

public class JsonEffectLoader
{
    private readonly EffectFormulaCompiler _formulaCompiler = new();
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        Converters = { new EffectActionDataConverter() },
        PropertyNameCaseInsensitive = true
    };

    public List<EffectRuntime> Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Effect JSON file not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var effectDefinitions = JsonSerializer.Deserialize<List<EffectDefinitionData>>(json, _serializerOptions) ?? [];

        var effectRuntimes = new List<EffectRuntime>();
        foreach (var effectDefinition in effectDefinitions)
        {
            var effectRuntime = CompileEffect(effectDefinition);
            effectRuntimes.Add(effectRuntime);
        }

        return effectRuntimes;
    }

    private EffectRuntime CompileEffect(EffectDefinitionData def)
    {
        var onApply = new List<Action<EffectContext>>();
        var onTick = new List<Action<EffectContext>>();
        var onExpire = new List<Action<EffectContext>>();

        foreach (var actionData in def.Actions)
        {
            var action = CompileAction(actionData);

            // sort by trigger
            switch (actionData.Trigger)
            {
                case "OnApply":
                    onApply.Add(action);
                    break;
                case "OnTick":
                    onTick.Add(action);
                    break;
                case "OnExpire":
                    onExpire.Add(action);
                break;
                default:
                    throw new NotSupportedException($"Unknown trigger '{actionData.Trigger}' in effect '{def.Name}'");
            }
        }

        // wear off message (add OnExpire action)
        if (def.WearOffMessage != null)
            onExpire.Add(ctx => ctx.Msg.To(ctx.Target).Send(def.WearOffMessage));
        // apply message
        if (def.ApplyMessage != null)
            onApply.Add(ctx => ctx.Msg.To(ctx.Target).Send(def.ApplyMessage));

        var tag = def.Tag == null
            ? EffectTagId.None
            : Enum.Parse<EffectTagId>(def.Tag, ignoreCase: true);
        var stacking = def.Stacking == null
            ? StackingRule.None
            : Enum.Parse<StackingRule>(def.Stacking, ignoreCase: true);
        var durationFunc = def.DurationFormula == null
            ? null
            : _formulaCompiler.Compile(def.DurationFormula);

        if (def.DurationFormula == null && (onTick.Count > 0 || onExpire.Count > 0))
            throw new Exception("DurationFormula must be specified when Trigger OnTick or OnExpire is defined");

        if (def.TickRate == 0 && onTick.Count > 0)
            throw new Exception("TickRate cannot be 0 when Trigger OnTick is defined");

        // TODO: duration must be specified when there is at least one StatModifierData

        return new EffectRuntime
        {
            Id = def.Name.ComputeUniqueId(),
            Name = def.Name,
            Tag = tag,
            Stacking = stacking,
            MaxStacks = def.MaxStacks,

            DurationFunc = durationFunc,

            TickOnApply = def.TickOnApply,
            TickRate = def.TickRate,

            OnApply = onApply.ToArray(),
            OnTick = onTick.ToArray(),
            OnExpire = onExpire.ToArray(),
        };
    }

    private Action<EffectContext> CompileAction(EffectActionData action)
    {
        switch (action)
        {
            case StatModifierData stat:
                {
                    var valueFunc = _formulaCompiler.Compile(stat.ValueFormula);
                    return ctx =>
                    {
                        var value = valueFunc(ctx); // TODO: multiply by stack count ?
                        var modifier = new StatModifier
                        {
                            Stat = Enum.Parse<StatKind>(stat.Stat, ignoreCase: true),
                            Kind = Enum.Parse<ModifierKind>(stat.Mode, ignoreCase: true),
                            Value = value
                        };

                        ref var statModifiers = ref ctx.Effect.TryGetRef<StatModifiers>(out var hasStatModifiers);
                        if (hasStatModifiers)
                            statModifiers.Values.Add(modifier);
                        else
                        {
                            ctx.Effect.Add(new StatModifiers
                            {
                                Values = [modifier]
                            });
                        }

                        // add dirty flag to character stats so we will recalculate them with the new modifiers
                        if (!ctx.Target.Has<DirtyStats>())
                            ctx.Target.Add<DirtyStats>();
                    };
                }

            case PeriodicHealData heal:
                {
                    var healFunc = _formulaCompiler.Compile(heal.HealFormula);
                    return ctx =>
                    {
                        var amount = healFunc(ctx); // TODO: should used snapshotted value
                        var totalHeal = amount * ctx.StackCount;
                        var healAction = new HealAction
                        {
                            Source = ctx.Source,
                            Target = ctx.Target,
                            Amount = totalHeal,
                            SourceKind = HealSourceKind.HoT
                        };
                        //ctx.Log.LogInformation(LogEvents.Hot, "Applying HoT heal for Effect {effectName} on Target {targetName} with heal {heal}", effect.DebugName, instance.Target.DebugName, totalHeal);
                        ctx.HealResolver.Resolve(ctx.State, healAction);
                    };
                }

            case InstantHealData heal:
                {
                    var healFunc = _formulaCompiler.Compile(heal.HealFormula);
                    return ctx =>
                    {
                        var amount = healFunc(ctx);
                        var totalHeal = amount;
                        var healAction = new HealAction
                        {
                            Source = ctx.Source,
                            Target = ctx.Target,
                            Amount = totalHeal,
                            SourceKind = HealSourceKind.Spell // TODO
                        };
                        ctx.HealResolver.Resolve(ctx.State, healAction);
                    };
                }

            case PeriodicDamageData dmg:
                {
                    var dmgFunc = _formulaCompiler.Compile(dmg.DamageFormula);
                    var dmgKind = Enum.Parse<DamageKind>(dmg.DamageKind, ignoreCase: true);
                    return ctx =>
                    {
                        var amount = dmgFunc(ctx); // TODO: should used snapshotted value
                        var totalDamage = amount * ctx.StackCount;
                        var damageAction = new DamageAction
                        {
                            Source = ctx.Source,
                            Target = ctx.Target,
                            Amount = totalDamage,
                            DamageKind = dmgKind,
                            SourceKind = DamageSourceKind.DoT
                        };
                        //_logger.LogInformation(LogEvents.Dot, "Applying DoT damage for Effect {effectName} on Target {targetName} with damage {damage} type {damageKind}", effect.DebugName, instance.Target.DebugName, totalDamage, damageEffect.DamageKind);
                        ctx.DamageResolver.Resolve(ctx.State, damageAction);
                    };
                }

            case InstantDamageData dmg:
                {
                    var dmgFunc = _formulaCompiler.Compile(dmg.DamageFormula);
                    var dmgKind = Enum.Parse<DamageKind>(dmg.DamageKind, ignoreCase: true);
                    return ctx =>
                    {
                        var amount = dmgFunc(ctx);
                        var totalDamage = amount;
                        var damageAction = new DamageAction
                        {
                            Source = ctx.Source,
                            Target = ctx.Target,
                            Amount = totalDamage,
                            DamageKind = dmgKind,
                            SourceKind = DamageSourceKind.Spell // TODO
                        };
                        ctx.DamageResolver.Resolve(ctx.State, damageAction);
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
