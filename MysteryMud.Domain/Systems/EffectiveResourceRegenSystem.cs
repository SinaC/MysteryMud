using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Systems;

public delegate void SetResourceRegenValueAction<TResourceRegen>(ref TResourceRegen resource, int value);

public class EffectiveResourceRegenSystem<TResourceRegen, TResourceDirtyRegen, TResourceRegenModifier>
    where TResourceRegen : struct
    where TResourceDirtyRegen : struct
    where TResourceRegenModifier : struct
{
    private readonly World _world;
    private readonly Func<TResourceRegen, int> _getBaseFunc;
    private readonly SetResourceRegenValueAction<TResourceRegen> _setCurrentAction;
    private readonly Func<TResourceRegenModifier, decimal> _getModifierValueFunc;
    private readonly Func<TResourceRegenModifier, ModifierKind> _getModifierKindFunc;

    public EffectiveResourceRegenSystem(World world, Func<TResourceRegen, int> getBaseFunc, SetResourceRegenValueAction<TResourceRegen> setCurrentAction, Func<TResourceRegenModifier, ModifierKind> getModifierKindFunc, Func<TResourceRegenModifier, decimal> getModifierValueFunc)
    {
        _world = world;
        _getBaseFunc = getBaseFunc;
        _setCurrentAction = setCurrentAction;
        _getModifierValueFunc = getModifierValueFunc;
        _getModifierKindFunc = getModifierKindFunc;
    }

    private static readonly QueryDescription _hasDirtyResourceRegenQueryDesc = new QueryDescription()
        .WithAll<TResourceRegen, TResourceDirtyRegen, CharacterEffects>()
        .WithNone<Dead>();

    public void Tick(GameState state)
    {
        _world.Query(_hasDirtyResourceRegenQueryDesc, (EntityId entity,
           ref TResourceRegen resourceRegen,
           ref TResourceDirtyRegen dirty,
           ref CharacterEffects characterEffects) => // TODO: other kind of entity
        {
            var baseRegen = _getBaseFunc(resourceRegen);

            // TODO: apply modifiers from equipment

            // apply modifiers from effects
            var (flat, percent, multiply, overriding) = ModifierPipeline.CalculateModifiers<CharacterResourceRegenModifiers<TResourceRegenModifier>, TResourceRegenModifier>(
                _world,
                characterEffects,
                x => true,
                x => x.Values,
                _getModifierKindFunc,
                _getModifierValueFunc);
           
            var rawCurrent = overriding ?? ((baseRegen + flat) * (100 + percent) * multiply / 100);

            // TODO: clamp ?

            // round final current
            int finalCurrent = (int)Math.Round(rawCurrent, MidpointRounding.AwayFromZero);

            // write back current resource regen
            _setCurrentAction(ref resourceRegen, finalCurrent);

            // remove dirty tag
            _world.Remove<TResourceDirtyRegen>(entity);
        });
    }
}
