using DefaultEcs;
using MysteryMud.Domain.Action.Effect.Helpers;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Helpers;

public static class ModifierPipeline
{
    public static (decimal flat, decimal percent, decimal multiply, decimal? overriding) CalculateModifiers<TModifiers, TModifier>(CharacterEffects characterEffects, Func<TModifier, bool> keepModifierFunc, Func<TModifiers, IEnumerable<TModifier>> getModifiersFunc, Func<TModifier, ModifierKind> getModifierKindFunc, Func<TModifier, decimal> getModifierValueFunc)
        => CalculateModifiers(characterEffects.Data.Effects, keepModifierFunc, getModifiersFunc, getModifierKindFunc, getModifierValueFunc);

    public static (decimal flat, decimal percent, decimal multiply, decimal? overriding) CalculateModifiers<TModifiers, TModifier>(ItemEffects itemEffects, Func<TModifier, bool> keepModifierFunc, Func<TModifiers, IEnumerable<TModifier>> getModifiersFunc, Func<TModifier, ModifierKind> getModifierKindFunc, Func<TModifier, decimal> getModifierValueFunc)
        => CalculateModifiers(itemEffects.Data.Effects, keepModifierFunc, getModifiersFunc, getModifierKindFunc, getModifierValueFunc);

    private static (decimal flat, decimal percent, decimal multiply, decimal? overriding) CalculateModifiers<TModifiers, TModifier>(IEnumerable<Entity> effects, Func<TModifier, bool> keepModifierFunc, Func<TModifiers, IEnumerable<TModifier>> getModifiersFunc, Func<TModifier, ModifierKind> getModifierKindFunc, Func<TModifier, decimal> getModifierValueFunc)
    {
        var flat = 0m;
        var percent = 0m;
        var multiply = 1m;
        var overriding = (decimal?)null;

        foreach (var effect in effects)
        {
            if (!EffectHelpers.IsAlive(effect))
                continue;

            // get modifiers
            if (!effect.Has<TModifiers>())
                continue;
            ref var modifiers = ref effect.Get<TModifiers>();

            ref var effectInstance = ref effect.Get<EffectInstance>();
            var stackCount = effectInstance.StackCount;
            foreach (var modifier in getModifiersFunc(modifiers))
            {
                if (!keepModifierFunc(modifier))
                    continue;

                var modifierValue = getModifierValueFunc(modifier) * stackCount;
                var modifierKind = getModifierKindFunc(modifier);
                switch (modifierKind)
                {
                    case ModifierKind.Flat:
                        flat += modifierValue;
                        break;
                    case ModifierKind.AddPercent: // AddPercent: 20 = +20%
                        percent += modifierValue;
                        break;
                    case ModifierKind.Multiply: // Multiply: 1.2 = x1.2
                        multiply *= modifierValue;
                        break;
                    case ModifierKind.Override: // what if multiple overrides? for now, just take the last one, but maybe we should prioritize by source (e.g. gear overrides > buff overrides > debuff overrides) or something like that
                        overriding = modifierValue;
                        break;
                }
            }
        }

        return (flat, percent, multiply, overriding);
    }

    // Accumulates modifiers for ALL stats in one pass into pre-allocated span buffers.
    // Avoids the O(stats × modifiers) cost of calling CalculateModifiers per stat.
    public static void AccumulateStatModifiers<TModifiers, TModifier>(
        IEnumerable<Entity> effects,
        Func<TModifier, CharacterStatKind> getStatFunc,
        Func<TModifiers, IEnumerable<TModifier>> getModifiersFunc,
        Func<TModifier, ModifierKind> getModifierKindFunc,
        Func<TModifier, decimal> getModifierValueFunc,
        Span<decimal> flat,
        Span<decimal> percent,
        Span<decimal> multiply,
        Span<decimal> overriding,
        Span<bool> hasOverriding)
    {
        foreach (var effect in effects)
        {
            if (!EffectHelpers.IsAlive(effect))
                continue;

            // get modifiers
            if (!effect.Has<TModifiers>())
                continue;
            ref var modifiers = ref effect.Get<TModifiers>();

            ref var effectInstance = ref effect.Get<EffectInstance>();
            var stackCount = effectInstance.StackCount;

            foreach (var modifier in getModifiersFunc(modifiers))
            {
                var statIndex = (int)getStatFunc(modifier);
                var value = getModifierValueFunc(modifier) * stackCount;

                switch (getModifierKindFunc(modifier))
                {
                    case ModifierKind.Flat: flat[statIndex] += value; break;
                    case ModifierKind.AddPercent: percent[statIndex] += value; break;
                    case ModifierKind.Multiply: multiply[statIndex] *= value; break;
                    case ModifierKind.Override:
                        overriding[statIndex] = value;
                        hasOverriding[statIndex] = true;
                        break;
                }
            }
        }
    }
}
