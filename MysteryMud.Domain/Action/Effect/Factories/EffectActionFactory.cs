using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core;
using MysteryMud.Domain.Action.Effect.Definitions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Factories;

public class EffectActionFactory
{
    private readonly ILogger _logger;

    public EffectActionFactory(ILogger logger)
    {
        _logger = logger;
    }

    public Action<EffectExecutionContext> Create(EffectActionDefinition actionDefinition) => actionDefinition switch
    {
        StatModifierActionDefinition definition => CreateStatModifier(definition),
        HealthModifierActionDefinition definition => CreateHealthModifier(definition),
        ManaModifierActionDefinition definition => CreateManaModifier(definition),
        EnergyModifierActionDefinition definition => CreateEnergyModifier(definition),
        RageModifierActionDefinition definition => CreateRageModifier(definition),
        PeriodicHealActionDefinition definition => CreatePeriodHeal(definition),
        InstantHealActionDefinition definition => CreateInstantHeal(definition),
        PeriodicDamageActionDefinition definition => CreatePeriodDamage(definition),
        InstantDamageActionDefinition definition => CreateInstantDamage(definition),
        ApplyTagActionDefinition definition => CreateApplyTag(definition),
        _ => throw new Exception($"Unknown EffectAction {actionDefinition.GetType()}"),
    };

    public Action<EffectExecutionContext> CreateStatModifier(StatModifierActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = definition.ValueFunc(ctx.Context); // TODO: multiply by stack count ?
                var modifier = new StatModifier
                {
                    Stat = definition.Stat,
                    Modifier = definition.Modifier,
                    Value = value
                };

                ref var statModifiers = ref effect.TryGetRef<StatModifiers>(out var hasStatModifiers);
                if (hasStatModifiers)
                    statModifiers.Values.Add(modifier);
                else
                {
                    effect.Add(new StatModifiers
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character stats so we will recalculate them with the new modifiers
                if (!effectContext.Target.Has<DirtyStats>())
                    effectContext.Target.Add<DirtyStats>();
            }
            else
                _logger.LogError("Trying to apply StatModifier on a null-effect");
        };
    }

    public Action<EffectExecutionContext> CreateHealthModifier(HealthModifierActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = definition.ValueFunc(ctx.Context); // TODO: multiply by stack count ?
                var modifier = new HealthModifier
                {
                    Modifier = definition.Modifier,
                    Value = value
                };

                ref var resourceModifiers = ref effect.TryGetRef<ResourceModifiers<HealthModifier>>(out var hasResourceModifiers);
                if (hasResourceModifiers)
                    resourceModifiers.Values.Add(modifier);
                else
                {
                    effect.Add(new ResourceModifiers<HealthModifier>
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character resources so we will recalculate them with the new modifiers
                if (!effectContext.Target.Has<DirtyHealth>())
                    effectContext.Target.Add<DirtyHealth>();
            }
            else
                _logger.LogError("Trying to apply HealthModifier on a null-effect");
        };
    }

    public Action<EffectExecutionContext> CreateManaModifier(ManaModifierActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = definition.ValueFunc(ctx.Context); // TODO: multiply by stack count ?
                var modifier = new ManaModifier
                {
                    Modifier = definition.Modifier,
                    Value = value
                };

                ref var resourceModifiers = ref effect.TryGetRef<ResourceModifiers<ManaModifier>>(out var hasResourceModifiers);
                if (hasResourceModifiers)
                    resourceModifiers.Values.Add(modifier);
                else
                {
                    effect.Add(new ResourceModifiers<ManaModifier>
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character resources so we will recalculate them with the new modifiers
                if (!effectContext.Target.Has<DirtyMana>())
                    effectContext.Target.Add<DirtyMana>();
            }
            else
                _logger.LogError("Trying to apply ManaModifier on a null-effect");
        };
    }

    public Action<EffectExecutionContext> CreateEnergyModifier(EnergyModifierActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = definition.ValueFunc(effectContext); // TODO: multiply by stack count ?
                var modifier = new EnergyModifier
                {
                    Modifier = definition.Modifier,
                    Value = value
                };

                ref var resourceModifiers = ref effect.TryGetRef<ResourceModifiers<EnergyModifier>>(out var hasResourceModifiers);
                if (hasResourceModifiers)
                    resourceModifiers.Values.Add(modifier);
                else
                {
                    effect.Add(new ResourceModifiers<EnergyModifier>
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character resources so we will recalculate them with the new modifiers
                if (!effectContext.Target.Has<DirtyEnergy>())
                    effectContext.Target.Add<DirtyEnergy>();
            }
            else
                _logger.LogError("Trying to apply EnergyModifier on a null-effect");
        };
    }

    public Action<EffectExecutionContext> CreateRageModifier(RageModifierActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = definition.ValueFunc(effectContext); // TODO: multiply by stack count ?
                var modifier = new RageModifier
                {
                    Modifier = definition.Modifier,
                    Value = value
                };

                ref var resourceModifiers = ref effect.TryGetRef<ResourceModifiers<RageModifier>>(out var hasResourceModifiers);
                if (hasResourceModifiers)
                    resourceModifiers.Values.Add(modifier);
                else
                {
                    effect.Add(new ResourceModifiers<RageModifier>
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character resources so we will recalculate them with the new modifiers
                if (!effectContext.Target.Has<DirtyRage>())
                    effectContext.Target.Add<DirtyRage>();
            }
            else
                _logger.LogError("Trying to apply RageModifier on a null-effect");
        };
    }

    public static Action<EffectExecutionContext> CreatePeriodHeal(PeriodicHealActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            var amount = definition.AmountFunc(effectContext); // TODO: should used snapshotted value
            var totalHeal = amount * effectContext.StackCount;
            var healAction = new HealAction
            {
                Source = effectContext.Source,
                Target = effectContext.Target,
                Amount = totalHeal,
                SourceKind = HealSourceKind.HoT
            };
            //ctx.Log.LogInformation(LogEvents.Hot, "Applying HoT heal for Effect {effectName} on Target {targetName} with heal {heal}", effect.DebugName, instance.Target.DebugName, totalHeal);
            ctx.Executor.ResolveHeal(effectContext.State, healAction);
        };
    }

    public static Action<EffectExecutionContext> CreateInstantHeal(InstantHealActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            var amount = definition.AmountFunc(effectContext);
            var totalHeal = amount;
            var healAction = new HealAction
            {
                Source = effectContext.Source,
                Target = effectContext.Target,
                Amount = totalHeal,
                SourceKind = HealSourceKind.Spell // TODO
            };
            ctx.Executor.ResolveHeal(effectContext.State, healAction);
        };
    }

    public static Action<EffectExecutionContext> CreatePeriodDamage(PeriodicDamageActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            var amount = definition.AmountFunc(effectContext); // TODO: should used snapshotted value
            var totalDamage = amount * effectContext.StackCount;
            var damageAction = new DamageAction
            {
                Source = effectContext.Source,
                Target = effectContext.Target,
                Amount = totalDamage,
                DamageKind = definition.Kind,
                SourceKind = DamageSourceKind.DoT
            };
            //_logger.LogInformation(LogEvents.Dot, "Applying DoT damage for Effect {effectName} on Target {targetName} with damage {damage} type {damageKind}", effect.DebugName, instance.Target.DebugName, totalDamage, damageEffect.DamageKind);
            ctx.Executor.ResolveDamage(effectContext.State, damageAction);
        };
    }

    public static Action<EffectExecutionContext> CreateInstantDamage(InstantDamageActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            var amount = definition.AmountFunc(effectContext);
            var totalDamage = amount;
            var damageAction = new DamageAction
            {
                Source = effectContext.Source,
                Target = effectContext.Target,
                Amount = totalDamage,
                DamageKind = definition.Kind,
                SourceKind = DamageSourceKind.Spell // TODO
            };
            ctx.Executor.ResolveDamage(effectContext.State, damageAction);
        };
    }

    public static Action<EffectExecutionContext> CreateApplyTag(ApplyTagActionDefinition definition)
    {
        return ctx =>
        {
            // NOP
            // TODO: for the moment effect tag is handled at effet level and not action level
        };
    }
}
