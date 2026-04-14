using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Systems;

public delegate void SetResourceRegenValueAction<TResourceRegen>(ref TResourceRegen resource, int value);

public class EffectiveResourceRegenSystem<TResourceRegen, TResourceDirtyRegen, TResourceRegenModifier>
    where TResourceRegen : struct
    where TResourceDirtyRegen : struct
    where TResourceRegenModifier : struct
{
    private readonly Func<TResourceRegen, int> _getBaseFunc;
    private readonly Func<TResourceRegen, int> _getCurrentFunc;
    private readonly SetResourceRegenValueAction<TResourceRegen> _setCurrentAction;
    private readonly Func<TResourceRegenModifier, decimal> _getModifierValueFunc;
    private readonly Func<TResourceRegenModifier, ModifierKind> _getModifierKindFunc;

    public EffectiveResourceRegenSystem(Func<TResourceRegen, int> getBaseFunc, Func<TResourceRegen, int> getCurrentFunc, SetResourceRegenValueAction<TResourceRegen> setCurrentAction, Func<TResourceRegenModifier, ModifierKind> getModifierKindFunc, Func<TResourceRegenModifier, decimal> getModifierValueFunc)
    {
        _getBaseFunc = getBaseFunc;
        _getCurrentFunc = getCurrentFunc;
        _setCurrentAction = setCurrentAction;
        _getModifierValueFunc = getModifierValueFunc;
        _getModifierKindFunc = getModifierKindFunc;
    }

    public void Tick(GameState state)
    {
        var query = new QueryDescription()
            .WithAll<TResourceRegen, TResourceDirtyRegen>()
            .WithNone<Dead>();
        state.World.Query(query, (Entity character,
            ref TResourceRegen resourceRegen,
            ref TResourceDirtyRegen resourceDirtyRegen) =>
        {
            var baseRegen = _getBaseFunc(resourceRegen);

            // TODO: apply modifiers from equipment

            // apply modifiers from effects
            var (flat, percent, multiply, overriding) = ModifierPipeline.CalculateModifiers<ResourceRegenModifiers<TResourceRegenModifier>, TResourceRegenModifier>(character, x => true, x => x.Values, _getModifierKindFunc, _getModifierValueFunc);
           
            var rawCurrent = overriding ?? ((baseRegen + flat) * (100 + percent) * multiply / 100);

            // TODO: clamp ?

            // round final current
            int finalCurrent = (int)Math.Round(rawCurrent, MidpointRounding.AwayFromZero);

            // write back current resource regen
            _setCurrentAction(ref resourceRegen, finalCurrent);

            // remove dirty tag
            character.Remove<TResourceDirtyRegen>();
        });
    }
}
