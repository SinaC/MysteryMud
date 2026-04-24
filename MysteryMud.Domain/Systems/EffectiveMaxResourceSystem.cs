using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Systems;

public delegate void ResourceValueSetter<TResource>(ref TResource resource, int value);

public class EffectiveMaxResourceSystem<TBase, TResource, TDirty, TModifier>
    where TBase : struct
    where TResource : struct
    where TDirty : struct
    where TModifier : struct
{
    private readonly World _world;
    private readonly Func<TBase, int> _getBaseMaxFunc;
    private readonly Func<TResource, int> _getCurrentFunc;
    private readonly ResourceValueSetter<TResource> _setCurrentAction;
    private readonly ResourceValueSetter<TResource> _setMaxAction;
    private readonly Func<TModifier, decimal> _getModifierValueFunc;
    private readonly Func<TModifier, ModifierKind> _getModifierKindFunc;

    public EffectiveMaxResourceSystem(World world, Func<TBase, int> getBaseMaxValueFunc, Func<TResource, int> getCurrentFunc, ResourceValueSetter<TResource> setCurrentAction, ResourceValueSetter<TResource> setMaxAction, Func<TModifier, ModifierKind> getModifierKindFunc, Func<TModifier, decimal> getModifierValueFunc)
    {
        _world = world;
        _getBaseMaxFunc = getBaseMaxValueFunc;
        _getCurrentFunc = getCurrentFunc;
        _setCurrentAction = setCurrentAction;
        _setMaxAction = setMaxAction;
        _getModifierValueFunc = getModifierValueFunc;
        _getModifierKindFunc = getModifierKindFunc;
    }

    private static readonly QueryDescription _hasDirtyResourceQueryDesc = new QueryDescription()
        .WithAll<TBase, TResource, TDirty, CharacterEffects>()
        .WithNone<Dead>();

    public void Tick(GameState state)
    {
        _world.Query(_hasDirtyResourceQueryDesc, (EntityId entity,
            ref TBase baseRes,
            ref TResource effectiveRes,
            ref TDirty _,
            ref CharacterEffects characterEffects) => // TODO: other kind of entity
        {
            // get base max
            int baseMax = _getBaseMaxFunc(baseRes);

            // TODO: apply modifiers from equipment

            // apply modifiers from effects
            var (flat, percent, multiply, overriding) = ModifierPipeline.CalculateModifiers<CharacterResourceModifiers<TModifier>, TModifier>(
                _world,
                characterEffects,
                x => true,
                x => x.Values,
                _getModifierKindFunc,
                _getModifierValueFunc);
           
            var rawMax = overriding ?? ((baseMax + flat) * (100 + percent) * multiply / 100);

            // TODO: clamp ?

            // round final max
            int finalMax = (int)Math.Round(rawMax, MidpointRounding.AwayFromZero);

            // clamp current value to new max
            int current = _getCurrentFunc(effectiveRes);
            current = Math.Min(current, finalMax);

            // write back effective resource
            _setMaxAction(ref effectiveRes, finalMax);
            _setCurrentAction(ref effectiveRes, current);

            // remove dirty tag
            _world.Remove<TDirty>(entity);
        });
    }
}
