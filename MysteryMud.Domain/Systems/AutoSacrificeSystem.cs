using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public class AutoSacrificeSystem
{
    private readonly IGameMessageService _msg;
    private readonly ISacrificeService _sacrificeService;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<ItemSacrificiedEvent> _itemSacrificiedEvents;

    public AutoSacrificeSystem(IGameMessageService msg, ISacrificeService sacrificeService, IIntentContainer intents, IEventBuffer<ItemSacrificiedEvent> itemSacrificiedEvents)
    {
        _msg = msg;
        _sacrificeService = sacrificeService;
        _intents = intents;
        _itemSacrificiedEvents = itemSacrificiedEvents;
    }

    public void Tick(GameState state)
    {
        foreach (ref var intent in _intents.AutoSacrificeSpan)
        {
            if (!CharacterHelpers.IsAlive(intent.Actor)) continue;
            if (!ItemHelpers.IsAlive(intent.Corpse)) continue;

            _sacrificeService.Sacrifice(intent.Actor, intent.Corpse);

            // event
            ref var itemDestroyedEvt = ref _itemSacrificiedEvents.Add();
            itemDestroyedEvt.Entity = intent.Actor;
            itemDestroyedEvt.Item = intent.Corpse;
        }
    }
}
