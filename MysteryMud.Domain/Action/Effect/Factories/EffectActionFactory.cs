using Microsoft.Extensions.Logging;
using MysteryMud.Core.Effects;
using MysteryMud.Domain.Ability.Resources;
using MysteryMud.Domain.Action.Effect.Definitions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Effect.Factories;

public class EffectActionFactory : IEffectActionFactory
{
    private readonly ILogger _logger;

    public EffectActionFactory(ILogger logger)
    {
        _logger = logger;
    }

    public Action<EffectExecutionContext> Create(EffectActionDefinition actionDefinition) => actionDefinition switch
    {
        CharacterStatModifierActionDefinition definition => CreateCharacterStatModifier(definition),
        ApplyCharacterTagActionDefinition definition => CreateApplyCharacterTag(definition),
        ApplyItemTagActionDefinition definition => CreateApplyItemTag(definition),
        HealthModifierActionDefinition definition => CreateResourceModifier<HealthModifier, DirtyHealth>(definition.Modifier, definition.ValueCompiledFormula, (modifier, value) => new HealthModifier { Modifier = modifier, Value = value }),
        HealthRegenModifierActionDefinition definition => CreateResourceRegenModifier<HealthRegenModifier, DirtyHealthRegen>(definition.Modifier, definition.ValueCompiledFormula, (modifier, value) => new HealthRegenModifier { Modifier = modifier, Value = value }),
        MoveModifierActionDefinition definition => CreateResourceModifier<MoveModifier, DirtyMove>(definition.Modifier, definition.ValueCompiledFormula, (modifier, value) => new MoveModifier { Modifier = modifier, Value = value }),
        MoveRegenModifierActionDefinition definition => CreateResourceRegenModifier<MoveRegenModifier, DirtyMoveRegen>(definition.Modifier, definition.ValueCompiledFormula, (modifier, value) => new MoveRegenModifier { Modifier = modifier, Value = value }),
        ResourceModifierActionDefinition definition => CreateResourceModifier(definition),
        ResourceRegenModifierActionDefinition definition => CreateResourceRegenModifier(definition),
        PeriodicHealActionDefinition definition => CreatePeriodHeal(definition),
        PeriodicDamageActionDefinition definition => CreatePeriodDamage(definition),
        InstantHealActionDefinition definition => CreateInstantHeal(definition),
        InstantDamageActionDefinition definition => CreateInstantDamage(definition),
        InstantRestoreMoveActionDefinition definition => CreateInstantRestoreMove(definition),
        InstantRestoreResourceActionDefinition definition => CreateInstantResourceResource(definition),
        _ => throw new Exception($"Unknown EffectAction {actionDefinition.GetType()}"),
    };

    private static void AddDirtyTag<TDirtyTag>(EffectContext effectContext)
        where TDirtyTag : struct
    {
        var target = effectContext.Target;
        if (target.Has<Equipped>())
            target = target.Get<Equipped>().Wearer;
        if (!target.Has<TDirtyTag>())
            target.Set<TDirtyTag>();
    }

    private Action<EffectExecutionContext> CreateCharacterStatModifier(CharacterStatModifierActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = definition.ValueCompiledFormula.Compiled(ctx.Context); // TODO: multiply by stack count ?
                var modifier = new CharacterStatModifier
                {
                    Stat = definition.Stat,
                    Modifier = definition.Modifier,
                    Value = value
                };

                if (effect.Has<CharacterStatModifiers>())
                {
                    ref var statModifiers = ref effect.Get<CharacterStatModifiers>();
                    statModifiers.Values.Add(modifier);
                }
                else
                {
                    effect.Set(new CharacterStatModifiers
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character (or wearer) stats so we will recalculate them with the new modifiers
                AddDirtyTag<DirtyStats>(effectContext);
            }
            else
                _logger.LogError("Trying to apply StatModifier on a null-effect");
        };
    }

    private Action<EffectExecutionContext> CreateHealthModifier(HealthModifierActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = definition.ValueCompiledFormula.Compiled(ctx.Context); // TODO: multiply by stack count ?
                var modifier = new HealthModifier
                {
                    Modifier = definition.Modifier,
                    Value = value
                };

                if (effect.Has<CharacterResourceModifiers<HealthModifier>>())
                {
                    ref var characterResourceModifiers = ref effect.Get<CharacterResourceModifiers<HealthModifier>>();
                    characterResourceModifiers.Values.Add(modifier);
                }
                else
                {
                    effect.Set(new CharacterResourceModifiers<HealthModifier>
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character (or wearer) resources so we will recalculate them with the new modifiers
                AddDirtyTag<DirtyHealth>(effectContext);
            }
            else
                _logger.LogError("Trying to apply HealthModifier on a null-effect");
        };
    }

    private Action<EffectExecutionContext> CreateHealthRegenModifier(HealthRegenModifierActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = definition.ValueCompiledFormula.Compiled(ctx.Context); // TODO: multiply by stack count ?
                var modifier = new HealthRegenModifier
                {
                    Modifier = definition.Modifier,
                    Value = value
                };

                if (effect.Has<CharacterResourceRegenModifiers<HealthRegenModifier>>())
                {
                    ref var characterResourceModifiers = ref effect.Get<CharacterResourceRegenModifiers<HealthRegenModifier>>();
                    characterResourceModifiers.Values.Add(modifier);
                }
                else
                {
                    effect.Set(new CharacterResourceRegenModifiers<HealthRegenModifier>
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character (or wearer) resources so we will recalculate them with the new modifiers
                AddDirtyTag<DirtyHealthRegen>(effectContext);
            }
            else
                _logger.LogError("Trying to apply HealthRegenModifier on a null-effect");
        };
    }

    private Action<EffectExecutionContext> CreateResourceModifier(ResourceModifierActionDefinition definition) => definition.Resource switch
    {
        ResourceKind.Mana => CreateResourceModifier<ManaModifier, DirtyMana>(definition.Modifier, definition.ValueCompiledFormula, (modifier, value) => new ManaModifier { Modifier = modifier, Value = value }),
        ResourceKind.Energy => CreateResourceModifier<EnergyModifier, DirtyEnergy>(definition.Modifier, definition.ValueCompiledFormula, (modifier, value) => new EnergyModifier { Modifier = modifier, Value = value }),
        ResourceKind.Rage => CreateResourceModifier<RageModifier, DirtyRage>(definition.Modifier, definition.ValueCompiledFormula, (modifier, value) => new RageModifier { Modifier = modifier, Value = value }),
        _ => _ => _logger.LogError("Trying to create ResourceModifier on unknown resource {resource}", definition.Resource)
    };

    private Action<EffectExecutionContext> CreateResourceModifier<TModifier, TDirty>(ModifierKind modifierKind, EffectCompiledFormula formula, Func<ModifierKind, decimal, TModifier> createModifierFunc)
        where TModifier: struct
        where TDirty : struct
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = formula.Compiled(ctx.Context); // TODO: multiply by stack count ?
                var modifier = createModifierFunc(modifierKind, value);

                if (effect.Has<CharacterResourceModifiers<TModifier>>())
                {
                    ref var resourceModifiers = ref effect.Get<CharacterResourceModifiers<TModifier>>();
                    resourceModifiers.Values.Add(modifier);
                }
                else
                {
                    effect.Set(new CharacterResourceModifiers<TModifier>
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character (or wearer) resources so we will recalculate them with the new modifiers
                AddDirtyTag<TDirty>(effectContext);
            }
            else
                _logger.LogError("Trying to apply {modifier} on a null-effect", typeof(TModifier).Name);
        };
    }

    private Action<EffectExecutionContext> CreateResourceRegenModifier(ResourceRegenModifierActionDefinition definition) => definition.Resource switch
    {
        ResourceKind.Mana => CreateResourceRegenModifier<ManaRegenModifier, DirtyManaRegen>(definition.Modifier, definition.ValueCompiledFormula, (modifier, value) => new ManaRegenModifier { Modifier = modifier, Value = value }),
        ResourceKind.Energy => CreateResourceRegenModifier<EnergyRegenModifier, DirtyEnergyRegen>(definition.Modifier, definition.ValueCompiledFormula, (modifier, value) => new EnergyRegenModifier { Modifier = modifier, Value = value }),
        ResourceKind.Rage => CreateResourceRegenModifier<RageDecayModifier, DirtyRageDecay>(definition.Modifier, definition.ValueCompiledFormula, (modifier, value) => new RageDecayModifier { Modifier = modifier, Value = value }),
        _ => _ => _logger.LogError("Trying to create ResourceModifier on unknown resource {resource}", definition.Resource)
    };

    private Action<EffectExecutionContext> CreateResourceRegenModifier<TRegenModifier, TDirtyRegen>(ModifierKind modifierKind, EffectCompiledFormula formula, Func<ModifierKind, decimal, TRegenModifier> createRegenModifierFunc)
        where TRegenModifier : struct
        where TDirtyRegen : struct
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = formula.Compiled(ctx.Context); // TODO: multiply by stack count ?
                var modifier = createRegenModifierFunc(modifierKind, value);

                if (effect.Has<CharacterResourceRegenModifiers<TRegenModifier>>())
                {
                    ref var resourceModifiers = ref effect.Get<CharacterResourceRegenModifiers<TRegenModifier>>();
                    resourceModifiers.Values.Add(modifier);
                }
                else
                {
                    effect.Set(new CharacterResourceRegenModifiers<TRegenModifier>
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character (or wearer) resources so we will recalculate them with the new modifiers
                AddDirtyTag<TDirtyRegen>(effectContext);
            }
            else
                _logger.LogError("Trying to apply {regenModifier} on a null-effect", typeof(TRegenModifier).Name);
        };
    }

    private static Action<EffectExecutionContext> CreatePeriodHeal(PeriodicHealActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            var amount = definition.AmountCompiledFormula.Compiled(effectContext);
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

    private static Action<EffectExecutionContext> CreateInstantHeal(InstantHealActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            var amount = definition.AmountCompiledFormula.Compiled(effectContext);
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

    private static Action<EffectExecutionContext> CreatePeriodDamage(PeriodicDamageActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            var amount = definition.AmountCompiledFormula.Compiled(effectContext);
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

    private static Action<EffectExecutionContext> CreateInstantDamage(InstantDamageActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            var amount = definition.AmountCompiledFormula.Compiled(effectContext);
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

    private static Action<EffectExecutionContext> CreateInstantRestoreMove(InstantRestoreMoveActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            var amount = definition.AmountCompiledFormula.Compiled(effectContext);
            var totalRestore = amount;
            var restoreMoveAction = new RestoreMoveAction
            {
                Source = effectContext.Source,
                Target = effectContext.Target,
                Amount = totalRestore
            };

            ctx.Executor.ResolveMove(effectContext.State, restoreMoveAction);
        };
    }

    private static Action<EffectExecutionContext> CreateInstantResourceResource(InstantRestoreResourceActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            var amount = definition.AmountCompiledFormula.Compiled(effectContext);

            ResourceHelpers.ModifyResource(effectContext.Target, definition.Resource, amount);
        };
    }

    private static Action<EffectExecutionContext> CreateApplyCharacterTag(ApplyCharacterTagActionDefinition definition)
    {
        return ctx =>
        {
            // NOP
            // TODO: for the moment effect tag is handled at effet level and not action level
        };
    }

    private static Action<EffectExecutionContext> CreateApplyItemTag(ApplyItemTagActionDefinition definition)
    {
        return ctx =>
        {
            // NOP
            // TODO: for the moment effect tag is handled at effet level and not action level
        };
    }
}
