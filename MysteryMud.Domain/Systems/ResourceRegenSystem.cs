using DefaultEcs;
using MysteryMud.Core;

namespace MysteryMud.Domain.Systems;

public class ResourceRegenSystem<TResource, TResourceRegen>
    where TResource : struct
    where TResourceRegen : struct
{
    private readonly Func<TResource, int> _getCurrentFunc;
    private readonly Func<TResource, int> _getMaxFunc;
    private readonly Func<TResourceRegen, int> _getRegenValueFunc;
    private readonly ResourceValueSetter<TResource> _setCurrentAction;
    private readonly EntitySet _resourceRegenEntitySet;

    public ResourceRegenSystem(World world, Func<TResource, int> getCurrentFunc, Func<TResource, int> getMaxFunc, Func<TResourceRegen, int> getRegenValueFunc, ResourceValueSetter<TResource> setCurrentAction)
    {
        _getCurrentFunc = getCurrentFunc;
        _getMaxFunc = getMaxFunc;
        _getRegenValueFunc = getRegenValueFunc;
        _setCurrentAction = setCurrentAction;
        _resourceRegenEntitySet = world
            .GetEntities()
            .With<TResource>()
            .With<TResourceRegen>()
            .AsSet();
    }

    public void Tick(GameState state)
    {
        foreach(var entity in _resourceRegenEntitySet.GetEntities())
        {
            ref var resource = ref entity.Get<TResource>();
            ref var regen = ref entity.Get<TResourceRegen>();
        
            var current = _getCurrentFunc(resource);
            var max = _getMaxFunc(resource);
            var regenValue = _getRegenValueFunc(regen);

            current = Math.Clamp(current + regenValue, 0, max);

            _setCurrentAction(ref resource, current);
        }
    }
}
