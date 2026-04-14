using Arch.Core;
using MysteryMud.Core;

namespace MysteryMud.Domain.Systems;

public class ResourceRegenSystem<TResource, TResourceRegen>
    where TResource : struct
    where TResourceRegen : struct
{
    private readonly Func<TResource, int> _getCurrentFunc;
    private readonly Func<TResource, int> _getMaxFunc;
    private readonly Func<TResourceRegen, int> _getRegenValueFunc;
    private readonly SetResourceValueAction<TResource> _setCurrentAction;

    public ResourceRegenSystem(Func<TResource, int> getCurrentFunc, Func<TResource, int> getMaxFunc, Func<TResourceRegen, int> getRegenValueFunc, SetResourceValueAction<TResource> setCurrentAction)
    {
        _getCurrentFunc = getCurrentFunc;
        _getMaxFunc = getMaxFunc;
        _getRegenValueFunc = getRegenValueFunc;
        _setCurrentAction = setCurrentAction;
    }

    public void Tick(GameState state)
    {
        var query = new QueryDescription()
            .WithAll<TResource, TResourceRegen>(); // don't check UsesResource, resource regen entity cannot use it for the moment
        state.World.Query(query, (Entity entity, ref TResource resource, ref TResourceRegen regen) =>
        {
            var current = _getCurrentFunc(resource);
            var max = _getMaxFunc(resource);
            var regenValue = _getRegenValueFunc(regen);

            current = Math.Clamp(current + regenValue, 0, max);
            _setCurrentAction(ref resource, current);
        });
    }
}
