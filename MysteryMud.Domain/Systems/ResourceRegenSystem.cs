using MysteryMud.Core;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Domain.Systems;

public class ResourceRegenSystem<TResource, TResourceRegen>
    where TResource : struct
    where TResourceRegen : struct
{
    private readonly World _world;
    private readonly Func<TResource, int> _getCurrentFunc;
    private readonly Func<TResource, int> _getMaxFunc;
    private readonly Func<TResourceRegen, int> _getRegenValueFunc;
    private readonly ResourceValueSetter<TResource> _setCurrentAction;

    public ResourceRegenSystem(World world, Func<TResource, int> getCurrentFunc, Func<TResource, int> getMaxFunc, Func<TResourceRegen, int> getRegenValueFunc, ResourceValueSetter<TResource> setCurrentAction)
    {
        _world = world;
        _getCurrentFunc = getCurrentFunc;
        _getMaxFunc = getMaxFunc;
        _getRegenValueFunc = getRegenValueFunc;
        _setCurrentAction = setCurrentAction;
    }

    private static readonly QueryDescription _hasResourceRegenQueryDesc = new QueryDescription()
        .WithAll<TResource, TResourceRegen>(); // don't check UsesResource, resource regen entity cannot use it for the moment


    public void Tick(GameState state)
    {
        _world.Query(_hasResourceRegenQueryDesc, (EntityId entity,
            ref TResource resource,
            ref TResourceRegen regen) =>
        {
            var current = _getCurrentFunc(resource);
            var max = _getMaxFunc(resource);
            var regenValue = _getRegenValueFunc(regen);

            current = Math.Clamp(current + regenValue, 0, max);
            _setCurrentAction(ref resource, current);
        });
    }
}
