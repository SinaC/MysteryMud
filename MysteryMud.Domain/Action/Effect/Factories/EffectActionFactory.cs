using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using MysteryMud.Core.Effects;
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
        HealthModifierActionDefinition definition => CreateHealthModifier(definition),
        HealthRegenModifierActionDefinition definition => CreateHealthRegenModifier(definition),
        ManaModifierActionDefinition definition => CreateManaModifier(definition),
        ManaRegenModifierActionDefinition definition => CreateManaRegenModifier(definition),
        EnergyModifierActionDefinition definition => CreateEnergyModifier(definition),
        EnergyRegenModifierActionDefinition definition => CreateEnergyRegenModifier(definition),
        RageModifierActionDefinition definition => CreateRageModifier(definition),
        RageRegenModifierActionDefinition definition => CreateRageRegenModifier(definition),
        PeriodicHealActionDefinition definition => CreatePeriodHeal(definition),
        InstantHealActionDefinition definition => CreateInstantHeal(definition),
        PeriodicDamageActionDefinition definition => CreatePeriodDamage(definition),
        InstantDamageActionDefinition definition => CreateInstantDamage(definition),
        _ => throw new Exception($"Unknown EffectAction {actionDefinition.GetType()}"),
    };

    private static void AddDirtyTag<TDirtyTag>(EffectContext effectContext)
        where TDirtyTag : struct
    {
        var target = effectContext.Target;
        ref var equipped = ref target.TryGetRef<Equipped>(out var isEquipped);
        if (isEquipped)
            target = equipped.Wearer;
        if (!target.Has<TDirtyTag>())
            target.Add<TDirtyTag>();
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

                ref var statModifiers = ref effect.TryGetRef<CharacterStatModifiers>(out var hasStatModifiers);
                if (hasStatModifiers)
                    statModifiers.Values.Add(modifier);
                else
                {
                    effect.Add(new CharacterStatModifiers
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

                ref var characterResourceModifiers = ref effect.TryGetRef<CharacterResourceModifiers<HealthModifier>>(out var hasCharacterResourceModifiers);
                if (hasCharacterResourceModifiers)
                    characterResourceModifiers.Values.Add(modifier);
                else
                {
                    effect.Add(new CharacterResourceModifiers<HealthModifier>
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

                ref var characterResourceModifiers = ref effect.TryGetRef<CharacterResourceRegenModifiers<HealthRegenModifier>>(out var hasCharacterResourceModifiers);
                if (hasCharacterResourceModifiers)
                    characterResourceModifiers.Values.Add(modifier);
                else
                {
                    effect.Add(new CharacterResourceRegenModifiers<HealthRegenModifier>
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

    private Action<EffectExecutionContext> CreateManaModifier(ManaModifierActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = definition.ValueCompiledFormula.Compiled(ctx.Context); // TODO: multiply by stack count ?
                var modifier = new ManaModifier
                {
                    Modifier = definition.Modifier,
                    Value = value
                };

                ref var resourceModifiers = ref effect.TryGetRef<CharacterResourceModifiers<ManaModifier>>(out var hasResourceModifiers);
                if (hasResourceModifiers)
                    resourceModifiers.Values.Add(modifier);
                else
                {
                    effect.Add(new CharacterResourceModifiers<ManaModifier>
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character (or wearer) resources so we will recalculate them with the new modifiers
                AddDirtyTag<DirtyMana>(effectContext);
            }
            else
                _logger.LogError("Trying to apply ManaModifier on a null-effect");
        };
    }

    private Action<EffectExecutionContext> CreateManaRegenModifier(ManaRegenModifierActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = definition.ValueCompiledFormula.Compiled(ctx.Context); // TODO: multiply by stack count ?
                var modifier = new ManaRegenModifier
                {
                    Modifier = definition.Modifier,
                    Value = value
                };

                ref var resourceModifiers = ref effect.TryGetRef<CharacterResourceRegenModifiers<ManaRegenModifier>>(out var hasResourceModifiers);
                if (hasResourceModifiers)
                    resourceModifiers.Values.Add(modifier);
                else
                {
                    effect.Add(new CharacterResourceRegenModifiers<ManaRegenModifier>
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character (or wearer) resources so we will recalculate them with the new modifiers
                AddDirtyTag<DirtyManaRegen>(effectContext);
            }
            else
                _logger.LogError("Trying to apply ManaRegenModifier on a null-effect");
        };
    }

    private Action<EffectExecutionContext> CreateEnergyModifier(EnergyModifierActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = definition.ValueCompiledFormula.Compiled(effectContext); // TODO: multiply by stack count ?
                var modifier = new EnergyModifier
                {
                    Modifier = definition.Modifier,
                    Value = value
                };

                ref var resourceModifiers = ref effect.TryGetRef<CharacterResourceModifiers<EnergyModifier>>(out var hasResourceModifiers);
                if (hasResourceModifiers)
                    resourceModifiers.Values.Add(modifier);
                else
                {
                    effect.Add(new CharacterResourceModifiers<EnergyModifier>
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character (or wearer) resources so we will recalculate them with the new modifiers
                AddDirtyTag<DirtyEnergy>(effectContext);
            }
            else
                _logger.LogError("Trying to apply EnergyModifier on a null-effect");
        };
    }

    private Action<EffectExecutionContext> CreateEnergyRegenModifier(EnergyRegenModifierActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = definition.ValueCompiledFormula.Compiled(effectContext); // TODO: multiply by stack count ?
                var modifier = new EnergyRegenModifier
                {
                    Modifier = definition.Modifier,
                    Value = value
                };

                ref var resourceModifiers = ref effect.TryGetRef<CharacterResourceRegenModifiers<EnergyRegenModifier>>(out var hasResourceModifiers);
                if (hasResourceModifiers)
                    resourceModifiers.Values.Add(modifier);
                else
                {
                    effect.Add(new CharacterResourceRegenModifiers<EnergyRegenModifier>
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character (or wearer) resources so we will recalculate them with the new modifiers
                AddDirtyTag<DirtyEnergyRegen>(effectContext);
            }
            else
                _logger.LogError("Trying to apply EnergyRegenModifier on a null-effect");
        };
    }

    private Action<EffectExecutionContext> CreateRageModifier(RageModifierActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = definition.ValueCompiledFormula.Compiled(effectContext); // TODO: multiply by stack count ?
                var modifier = new RageModifier
                {
                    Modifier = definition.Modifier,
                    Value = value
                };

                ref var resourceModifiers = ref effect.TryGetRef<CharacterResourceModifiers<RageModifier>>(out var hasResourceModifiers);
                if (hasResourceModifiers)
                    resourceModifiers.Values.Add(modifier);
                else
                {
                    effect.Add(new CharacterResourceModifiers<RageModifier>
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character (or wearer) resources so we will recalculate them with the new modifiers
                AddDirtyTag<DirtyRage>(effectContext);
            }
            else
                _logger.LogError("Trying to apply RageModifier on a null-effect");
        };
    }
    private Action<EffectExecutionContext> CreateRageRegenModifier(RageRegenModifierActionDefinition definition)
    {
        return ctx =>
        {
            var effectContext = ctx.Context;
            if (effectContext.Effect is not null)
            {
                var effect = effectContext.Effect.Value;
                var value = definition.ValueCompiledFormula.Compiled(effectContext); // TODO: multiply by stack count ?
                var modifier = new RageDecayModifier
                {
                    Modifier = definition.Modifier,
                    Value = value
                };

                ref var resourceModifiers = ref effect.TryGetRef<CharacterResourceRegenModifiers<RageDecayModifier>>(out var hasResourceModifiers);
                if (hasResourceModifiers)
                    resourceModifiers.Values.Add(modifier);
                else
                {
                    effect.Add(new CharacterResourceModifiers<RageDecayModifier>
                    {
                        Values = [modifier]
                    });
                }

                // add dirty flag to character (or wearer) resources so we will recalculate them with the new modifiers
                AddDirtyTag<DirtyRageDecay>(effectContext);
            }
            else
                _logger.LogError("Trying to apply RageDecayModifier on a null-effect");
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
