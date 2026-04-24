using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Events;
using TinyECS;

namespace MysteryMud.Domain.Systems;

public class AutoSacrificeSystem
{
    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly ISacrificeService _sacrificeService;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<ItemSacrificiedEvent> _itemSacrificiedEvents;

    public AutoSacrificeSystem(World world, IGameMessageService msg, ISacrificeService sacrificeService, IIntentContainer intents, IEventBuffer<ItemSacrificiedEvent> itemSacrificiedEvents)
    {
        _world = world;
        _msg = msg;
        _sacrificeService = sacrificeService;
        _intents = intents;
        _itemSacrificiedEvents = itemSacrificiedEvents;
    }

    public void Tick(GameState state)
    {
        foreach (ref var intent in _intents.AutoSacrificeSpan)
        {
            if (!CharacterHelpers.IsAlive(_world, intent.Actor)) continue;
            if (!ItemHelpers.IsAlive(_world, intent.Corpse)) continue;

            _sacrificeService.Sacrifice(intent.Actor, intent.Corpse);

            // event
            ref var itemDestroyedEvt = ref _itemSacrificiedEvents.Add();
            itemDestroyedEvt.Entity = intent.Actor;
            itemDestroyedEvt.Item = intent.Corpse;
        }
    }
}
