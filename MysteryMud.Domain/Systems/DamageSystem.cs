using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Domain.Combat.Resolvers;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public sealed class DamageSystem
{
    private readonly DamageResolver _damageResolver;
    private readonly IEventBuffer<DamageEvent> _damages;

    public DamageSystem(DamageResolver damageResolver, IEventBuffer<DamageEvent> damages)
    {
        _damageResolver = damageResolver;
        _damages = damages;
    }

    public void Tick(GameState state)
    {
        foreach (ref var dmg in _damages.GetAll())
        {
            _damageResolver.Resolve(dmg);
        }
    }
}
