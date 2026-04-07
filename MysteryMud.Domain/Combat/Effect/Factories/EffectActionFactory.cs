using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Domain.Combat.Damage;
using MysteryMud.Domain.Combat.Effect.Definitions;
using MysteryMud.Domain.Combat.Heal;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Combat.Effect.Factories;

public class EffectActionFactory
{
    private readonly ILogger _logger;

    public EffectActionFactory(ILogger logger)
    {
        _logger = logger;
    }

    public Action<EffectContext> Create(EffectActionDefinition actionDefinition) => actionDefinition switch
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
        _ => throw new Exception($"Unknown EffectAction {actionDefinition.GetType()}"),
    };

    public Action<EffectContext> CreateStatModifier(StatModifierActionDefinition definition)
    {
        return ctx =>
        {
            if (ctx.Effect is not null)
            {
                var effect = ctx.Effect.Value;
                var value = definition.ValueFunc(ctx); // TODO: multiply by stack count ?
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
                if (!ctx.Target.Has<DirtyStats>())
                    ctx.Target.Add<DirtyStats>();
            }
            else
                _logger.LogError("Trying to apply StatModifier on a null-effect");
        };
    }

    public Action<EffectContext> CreateHealthModifier(HealthModifierActionDefinition definition)
    {
        return ctx =>
        {
            if (ctx.Effect is not null)
            {
                var effect = ctx.Effect.Value;
                var value = definition.ValueFunc(ctx); // TODO: multiply by stack count ?
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
                if (!ctx.Target.Has<DirtyHealth>())
                    ctx.Target.Add<DirtyHealth>();
            }
            else
                _logger.LogError("Trying to apply HealthModifier on a null-effect");
        };
    }

    public Action<EffectContext> CreateManaModifier(ManaModifierActionDefinition definition)
    {
        return ctx =>
        {
            if (ctx.Effect is not null)
            {
                var effect = ctx.Effect.Value;
                var value = definition.ValueFunc(ctx); // TODO: multiply by stack count ?
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
                if (!ctx.Target.Has<DirtyMana>())
                    ctx.Target.Add<DirtyMana>();
            }
            else
                _logger.LogError("Trying to apply ManaModifier on a null-effect");
        };
    }

    public Action<EffectContext> CreateEnergyModifier(EnergyModifierActionDefinition definition)
    {
        return ctx =>
        {
            if (ctx.Effect is not null)
            {
                var effect = ctx.Effect.Value;
                var value = definition.ValueFunc(ctx); // TODO: multiply by stack count ?
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
                if (!ctx.Target.Has<DirtyEnergy>())
                    ctx.Target.Add<DirtyEnergy>();
            }
            else
                _logger.LogError("Trying to apply EnergyModifier on a null-effect");
        };
    }

    public Action<EffectContext> CreateRageModifier(RageModifierActionDefinition definition)
    {
        return ctx =>
        {
            if (ctx.Effect is not null)
            {
                var effect = ctx.Effect.Value;
                var value = definition.ValueFunc(ctx); // TODO: multiply by stack count ?
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
                if (!ctx.Target.Has<DirtyRage>())
                    ctx.Target.Add<DirtyRage>();
            }
            else
                _logger.LogError("Trying to apply RageModifier on a null-effect");
        };
    }

    public static Action<EffectContext> CreatePeriodHeal(PeriodicHealActionDefinition definition)
    {
        return ctx =>
        {
            var amount = definition.AmountFunc(ctx); // TODO: should used snapshotted value
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

    public static Action<EffectContext> CreateInstantHeal(InstantHealActionDefinition definition)
    {
        return ctx =>
        {
            var amount = definition.AmountFunc(ctx);
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

    public static Action<EffectContext> CreatePeriodDamage(PeriodicDamageActionDefinition definition)
    {
        return ctx =>
        {
            var amount = definition.AmountFunc(ctx); // TODO: should used snapshotted value
            var totalDamage = amount * ctx.StackCount;
            var damageAction = new DamageAction
            {
                Source = ctx.Source,
                Target = ctx.Target,
                Amount = totalDamage,
                DamageKind = definition.Kind,
                SourceKind = DamageSourceKind.DoT
            };
            //_logger.LogInformation(LogEvents.Dot, "Applying DoT damage for Effect {effectName} on Target {targetName} with damage {damage} type {damageKind}", effect.DebugName, instance.Target.DebugName, totalDamage, damageEffect.DamageKind);
            ctx.DamageResolver.Resolve(ctx.State, damageAction);
        };
    }

    public static Action<EffectContext> CreateInstantDamage(InstantDamageActionDefinition definition)
    {
        return ctx =>
        {
            var amount = definition.AmountFunc(ctx);
            var totalDamage = amount;
            var damageAction = new DamageAction
            {
                Source = ctx.Source,
                Target = ctx.Target,
                Amount = totalDamage,
                DamageKind = definition.Kind,
                SourceKind = DamageSourceKind.Spell // TODO
            };
            ctx.DamageResolver.Resolve(ctx.State, damageAction);
        };
    }
}
