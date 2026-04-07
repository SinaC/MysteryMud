using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Systems;

public delegate void SetResourceValueAction<TResource>(ref TResource rsource, int value);

public class MaxResourcesSystem<TBase, TResource, TDirty, TModifier>
    where TBase : struct
    where TResource : struct
    where TDirty : struct
    where TModifier : struct
{
    private Func<TBase, int> _getBaseMaxFunc;
    private Func<TResource, int> _getCurrentFunc;
    private SetResourceValueAction<TResource> _setCurrentAction;
    private SetResourceValueAction<TResource> _setMaxAction;
    private Func<TModifier, decimal> _getModifierValueFunc;
    private Func<TModifier, ModifierKind> _getModifierKindFunc;

    public MaxResourcesSystem(Func<TBase, int> getBaseMaxValueFunc, Func<TResource, int> getCurrentFunc, SetResourceValueAction<TResource> setCurrentAction, SetResourceValueAction<TResource> setMaxAction, Func<TModifier, ModifierKind> getModifierKindFunc, Func<TModifier, decimal> getModifierValueFunc)
    {
        _getBaseMaxFunc = getBaseMaxValueFunc;
        _getCurrentFunc = getCurrentFunc;
        _setCurrentAction = setCurrentAction;
        _setMaxAction = setMaxAction;
        _getModifierValueFunc = getModifierValueFunc;
        _getModifierKindFunc = getModifierKindFunc;
    }

    public void Tick(GameState state)
    {
        var query = new QueryDescription()
            .WithAll<TBase, TResource, TDirty>()
            .WithNone<Dead>();
        state.World.Query(query, (Entity character,
            ref TBase baseRes,
            ref TResource effectiveRes,
            ref TDirty dirty) =>
        {
            // get base max
            int baseMax = _getBaseMaxFunc(baseRes);

            // TODO: apply modifiers from equipment

            // apply modifiers from effects
            var flat = 0m;
            var percent = 0m;
            var multiply = 1m;
            var overriding = (decimal?)null;

            decimal newMax = baseMax;
            ref var characterEffects = ref character.Get<CharacterEffects>();
            foreach (var effect in characterEffects.Effects)
            {
                if (!EffectHelpers.IsAlive(effect))
                    continue;

                // get modifiers
                ref var effectModifiers = ref effect.TryGetRef<ResourceModifiers<TModifier>>(out var hasModifiers);
                if (!hasModifiers)
                    continue;

                ref var effectInstance = ref effect.Get<EffectInstance>();
                var stackCount = effectInstance.StackCount;
                foreach (var modifier in effectModifiers.Values)
                {
                    var modifierValue = _getModifierValueFunc(modifier) * stackCount;
                    var modifierKind = _getModifierKindFunc(modifier);
                    switch (modifierKind)
                    {
                        case ModifierKind.Flat:
                            flat += modifierValue;
                            break;
                        case ModifierKind.AddPercent:
                            percent += modifierValue;
                            break;
                        case ModifierKind.Multiply:
                            multiply *= modifierValue;
                            break;
                        case ModifierKind.Override: // what if multiple overrides? for now, just take the last one, but maybe we should prioritize by source (e.g. gear overrides > buff overrides > debuff overrides) or something like that
                            overriding = modifierValue;
                            break;
                    }
                }
            }
            newMax = overriding ?? ((baseMax + flat) * (100 + percent) * multiply / 100);

            // round final max
            int finalMax = (int)Math.Round(newMax, MidpointRounding.AwayFromZero);

            // clamp current value
            int current = _getCurrentFunc(effectiveRes);
            current = Math.Min(current, finalMax);

            // write back effective resource
            _setMaxAction(ref effectiveRes, finalMax);
            _setCurrentAction(ref effectiveRes, current);

            // remove dirty tag
            character.Remove<TDirty>();
        });
    }
}
