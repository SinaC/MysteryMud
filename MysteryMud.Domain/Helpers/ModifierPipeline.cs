using Arch.Core;
using Arch.Core.Extensions;
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
            ref var modifiers = ref effect.TryGetRef<TModifiers>(out var hasModifiers);
            if (!hasModifiers)
                continue;

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
}
