using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Domain.Action.Effect;
using MysteryMud.Domain.Action.Effect.Definitions;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Time;

namespace MysteryMud.Application.Services;

public class EffectDisplayService : IEffectDisplayService
{
    private readonly IGameMessageService _msg;
    private readonly IEffectRegistry _effectRegistry;

    public EffectDisplayService(IGameMessageService msg, IEffectRegistry effectRegistry)
    {
        _msg = msg;
        _effectRegistry = effectRegistry;
    }

    public void DisplayEffects(GameState state, Entity viewer, List<Entity> effects)
    {
        if (effects.Count == 0)
        {
            _msg.To(viewer).Send($"No effects");
            return;
        }
        _msg.To(viewer).Send($"Effects:");
        foreach (var effect in effects)
        {
            DisplayEffect(state, viewer, effect);
        }
    }

    private void DisplayEffect(GameState state, Entity viewer, Entity effect)
    {
        if (!effect.IsAlive || effect.Has<ExpiredTag>())
            return;
        ref var effectInstance = ref effect.Get<EffectInstance>();
        if (effectInstance.EffectRuntime != null)
        {
            var effectId = effectInstance.EffectRuntime.Id;
            var effectName = effectInstance.EffectRuntime.Name;
            var stackCount = effectInstance.StackCount;
            var source = effectInstance.Source;
            var target = effectInstance.Target;
            var sourceName = source.DisplayName;

            // get effect definition
            _effectRegistry.TryGetDefinition(effectId, out var effectDefinition);

            // duration
            if (effect.Has<TimedEffect>())
            {
                ref var timedEffect = ref effect.Get<TimedEffect>();
                var remainingTicks = timedEffect.ExpirationTick - state.CurrentTick;
                _msg.To(viewer).Send($"- {effectName} Source: {sourceName} Stacks: {stackCount} Remaining time: {TimeConversion.TicksToDisplay(remainingTicks)}");
            }
            else
                _msg.To(viewer).Send($"- {effectName} Source: {sourceName} Stacks: {stackCount} Permanent");

            // tick
            if (effectInstance.EffectRuntime.OnTick.Length > 0 && effectDefinition is not null)
            {
                foreach (var tickAction in effectDefinition.Actions.Where(x => x.Trigger == TriggerType.OnTick))
                {
                    switch (tickAction)
                    {
                        case PeriodicDamageActionDefinition periodic:
                            {
                                var amount = EvaluateForDisplay(periodic.AmountCompiledFormula, effect, ref effectInstance, ref source, ref target, state);
                                _msg.To(viewer).Send($"  - {amount} {periodic.Kind} damage every {TimeConversion.TicksToDisplay(effectDefinition.TickRate)}");
                                break;
                            }
                        case PeriodicHealActionDefinition periodic:
                            {
                                var amount = EvaluateForDisplay(periodic.AmountCompiledFormula, effect, ref effectInstance, ref source, ref target, state);
                                _msg.To(viewer).Send($"  - {amount} heal every {TimeConversion.TicksToDisplay(effectDefinition.TickRate)}");
                                break;
                            }
                    }
                }
            }

            // character stat modifiers
            if (effect.Has<CharacterStatModifiers>())
            {
                ref var characterStatModifiers = ref effect.Get<CharacterStatModifiers>();
                foreach (var modifier in characterStatModifiers.Values)
                    _msg.To(viewer).Send($"  - {modifier.Modifier} {modifier.Value} {modifier.Stat}");
            }

            // resource modifiers
            DisplayResourceModifier<HealthModifier>(viewer, effect, "Health", x => x.Modifier, x => x.Value);
            DisplayResourceModifier<MoveModifier>(viewer, effect, "Move", x => x.Modifier, x => x.Value);
            DisplayResourceModifier<ManaModifier>(viewer, effect, "Mana", x => x.Modifier, x => x.Value);
            DisplayResourceModifier<EnergyModifier>(viewer, effect, "Energy", x => x.Modifier, x => x.Value);
            DisplayResourceModifier<RageModifier>(viewer, effect, "Rage", x => x.Modifier, x => x.Value);

            // resource regen modifiers
            DisplayResourceRegenModifier<HealthRegenModifier>(viewer, effect, "Health", x => x.Modifier, x => x.Value);
            DisplayResourceRegenModifier<MoveRegenModifier>(viewer, effect, "Move", x => x.Modifier, x => x.Value);
            DisplayResourceRegenModifier<ManaRegenModifier>(viewer, effect, "Mana ", x => x.Modifier, x => x.Value);
            DisplayResourceRegenModifier<EnergyRegenModifier>(viewer, effect, "Energy", x => x.Modifier, x => x.Value);
            DisplayResourceRegenModifier<RageDecayModifier>(viewer, effect, "Rage", x => x.Modifier, x => x.Value);

            // TODO: expire ?
        }
    }

    private void DisplayResourceModifier<TResourceModifier>(Entity viewer, Entity effect, string resourceName, Func<TResourceModifier, ModifierKind> getModifierFunc, Func<TResourceModifier, decimal> getValueFunc)
        where TResourceModifier : struct
    {
        if (effect.Has<CharacterResourceModifiers<TResourceModifier>>())
        {
            ref var resourceModifiers = ref effect.Get<CharacterResourceModifiers<TResourceModifier>>();
            foreach (var modifier in resourceModifiers.Values)
                _msg.To(viewer).Send($"  - {getModifierFunc(modifier)} {getValueFunc(modifier)} {resourceName}");
        }
    }

    private void DisplayResourceRegenModifier<TResourceRegenModifier>(Entity viewer, Entity effect, string resourceName, Func<TResourceRegenModifier, ModifierKind> getModifierFunc, Func<TResourceRegenModifier, decimal> getValueFunc)
        where TResourceRegenModifier : struct
    {
        if (effect.Has<CharacterResourceRegenModifiers<TResourceRegenModifier>>())
        {
            ref var resourceRegenModifiers = ref effect.Get<CharacterResourceRegenModifiers<TResourceRegenModifier>>();
            foreach (var modifier in resourceRegenModifiers.Values)
                _msg.To(viewer).Send($"  - {getModifierFunc(modifier)} {getValueFunc(modifier)} {resourceName} regen");
        }
    }

    public static decimal EvaluateForDisplay(
        EffectCompiledFormula compiledFormula,
        Entity effectEntity,
        ref EffectInstance effectInstance,
        ref Entity source,
        ref Entity target,
        GameState state)
    {
        var ctx = new EffectContext
        {
            Effect = effectEntity,
            Source = source,
            Target = target,
            StackCount = effectInstance.StackCount,
            EffectiveDamageAmount = 0, // unknown at display time, formula will use 0
            State = state,
        };

        return compiledFormula.Compiled(ctx);
    }
}
