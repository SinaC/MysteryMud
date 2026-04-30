using DefaultEcs;
using MysteryMud.Domain.Action.Effect.Helpers;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Helpers;

public static class FlagModifierPipeline
{
    public static (ulong or, ulong nor, ulong? overriding) CalculateModifiers<TFlagModifiers, TFlagModifier>(CharacterEffects characterEffects, Func<TFlagModifier, bool> keepModifierFunc, Func<TFlagModifiers, IEnumerable<TFlagModifier>> getModifiersFunc, Func<TFlagModifier, FlagModifierKind> getModifierKindFunc, Func<TFlagModifier, ulong> getModifierValueFunc)
        => CalculateModifiers(characterEffects.Data.Effects, keepModifierFunc, getModifiersFunc, getModifierKindFunc, getModifierValueFunc);

    public static (ulong or, ulong nor, ulong? overriding) CalculateModifiers<TFlagModifiers, TFlagModifier>(ItemEffects itemEffects, Func<TFlagModifier, bool> keepModifierFunc, Func<TFlagModifiers, IEnumerable<TFlagModifier>> getModifiersFunc, Func<TFlagModifier, FlagModifierKind> getModifierKindFunc, Func<TFlagModifier, ulong> getModifierValueFunc)
        => CalculateModifiers(itemEffects.Data.Effects, keepModifierFunc, getModifiersFunc, getModifierKindFunc, getModifierValueFunc);

    private static (ulong or, ulong nor, ulong? overriding) CalculateModifiers<TFlagModifiers, TFlagModifier>(IEnumerable<Entity> effects, Func<TFlagModifier, bool> keepModifierFunc, Func<TFlagModifiers, IEnumerable<TFlagModifier>> getModifiersFunc, Func<TFlagModifier, FlagModifierKind> getModifierKindFunc, Func<TFlagModifier, ulong> getModifierValueFunc)
    {
        var or = 0UL;
        var nor = 0UL;
        var overriding = (ulong?)null;

        foreach (var effect in effects)
        {
            if (!EffectHelpers.IsAlive(effect))
                continue;

            // get flag modifiers
            if (!effect.Has<TFlagModifiers>())
                continue;
            ref var modifiers = ref effect.Get<TFlagModifiers>();

            ref var effectInstance = ref effect.Get<EffectInstance>();
            foreach (var modifier in getModifiersFunc(modifiers))
            {
                if (!keepModifierFunc(modifier))
                    continue;

                var modifierValue = getModifierValueFunc(modifier);
                var modifierKind = getModifierKindFunc(modifier);
                switch (modifierKind)
                {
                    case FlagModifierKind.Or:
                        or |= modifierValue;
                        break;
                    case FlagModifierKind.Nor:
                        nor |= modifierValue;
                        break;
                    case FlagModifierKind.Override: // what if multiple overrides? for now, just take the last one, but maybe we should prioritize by source (e.g. gear overrides > buff overrides > debuff overrides) or something like that
                        overriding = modifierValue;
                        break;
                }
            }
        }

        return (or, nor, overriding);
    }
}
