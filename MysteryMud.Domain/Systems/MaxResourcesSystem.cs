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
    private readonly Func<TBase, int> _getBaseMaxFunc;
    private readonly Func<TResource, int> _getCurrentFunc;
    private readonly SetResourceValueAction<TResource> _setCurrentAction;
    private readonly SetResourceValueAction<TResource> _setMaxAction;
    private readonly Func<TModifier, decimal> _getModifierValueFunc;
    private readonly Func<TModifier, ModifierKind> _getModifierKindFunc;

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
            var (flat, percent, multiply, overriding) = ModifierPipeline.CalculateModifiers<ResourceModifiers<TModifier>, TModifier>(character, x => true, x => x.Values, _getModifierKindFunc, _getModifierValueFunc);
           
            var rawMax = overriding ?? ((baseMax + flat) * (100 + percent) * multiply / 100);

            // round final max
            int finalMax = (int)Math.Round(rawMax, MidpointRounding.AwayFromZero);

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
