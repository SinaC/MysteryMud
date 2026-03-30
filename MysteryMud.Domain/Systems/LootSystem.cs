using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Systems;

public sealed class LootSystem
{
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intents;
    private readonly IEventBuffer<ItemLootedEvent> _itemLootedEvents;

    public LootSystem(IGameMessageService msg, IIntentContainer intents, IEventBuffer<ItemLootedEvent> itemLootedEvents)
    {
        _msg = msg;
        _intents = intents;
        _itemLootedEvents = itemLootedEvents;
    }

    public void Tick(GameState state)
    {
        foreach (ref var intent in _intents.LootSpan)
        {
            ref var containerContent = ref intent.Corpse.Get<ContainerContents>();
            foreach (var content in containerContent.Items.ToArray())
            {
                // get item, add to inventory, remove from corpse
                if (ItemHelpers.TryGetItemFromContainer(intent.Looter, intent.Corpse, content, out var reason))
                {
                    _msg.To(intent.Looter).Send($"You loot {content.DisplayName} from {intent.Corpse.DisplayName}.");

                    // item looted event
                    ref var itemLootedEvt = ref _itemLootedEvents.Add();
                    itemLootedEvt.Entity = intent.Looter;
                    itemLootedEvt.Item = content;
                    itemLootedEvt.Corpse = intent.Corpse;
                }
            }
        }
    }
}
