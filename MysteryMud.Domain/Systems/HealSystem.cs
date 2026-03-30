using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Domain.Combat.Resolvers;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public sealed class HealSystem
{
    private readonly HealResolver _healResolver;
    private readonly IEventBuffer<HealEvent> _heals;

    public HealSystem(HealResolver healResolver, IEventBuffer<HealEvent> heals)
    {
        _healResolver = healResolver;
        _heals = heals;
    }

    public void Tick(GameState state)
    {
        foreach (ref var heal in _heals.GetAll())
        {
            _healResolver.Resolve(heal);
        }
    }
}
