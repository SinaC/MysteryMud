using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Eventing;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Events;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Domain.Systems;

public class ItemInteractionSystem
{
    private readonly IGameMessageService _msg;
    private readonly IIntentContainer _intentContainer;
    private readonly IEventBuffer<ItemGotEvent> _itemGotEvents;
    private readonly IEventBuffer<ItemDroppedEvent> _itemDroppedEvents;
    private readonly IEventBuffer<ItemGivenEvent> _itemGivenEvents;
    private readonly IEventBuffer<ItemPutEvent> _itemPutEvents;

    public ItemInteractionSystem(IGameMessageService gameMessageService, IIntentContainer intentContainer, IEventBuffer<ItemGotEvent> itemGotEvents, IEventBuffer<ItemDroppedEvent> itemDroppedEvents, IEventBuffer<ItemGivenEvent> itemGivenEvents, IEventBuffer<ItemPutEvent> itemPutEvents)
    {
        _msg = gameMessageService;
        _intentContainer = intentContainer;
        _itemGotEvents = itemGotEvents;
        _itemDroppedEvents = itemDroppedEvents;
        _itemGivenEvents = itemGivenEvents;
        _itemPutEvents = itemPutEvents;
    }

    public void Tick(GameState state)
    {
        // handle get/drop/give/put/wear/remove
        foreach (var getIntent in _intentContainer.GetItemSpan)
            HandleGet(state, getIntent);
        foreach (var dropIntent in _intentContainer.DropItemSpan)
            HandleDrop(state, dropIntent);
        foreach (var giveIntent in _intentContainer.GiveItemSpan)
            HandleGive(state, giveIntent);
        foreach (var putIntent in _intentContainer.PutItemSpan)
            HandlePut(state, putIntent);
        // TODO: wear/remove
    }

    private void HandleGet(GameState state, GetItemIntent intent)
    {
        // TODO: validation

        // get item from room/container and put it in entity's inventory
        if (intent.RoomOrContainer.Has<RoomContents>())
            ItemHelpers.TryGetItemFromRoom(intent.Entity, intent.RoomOrContainer, intent.Item, out _);
        else if (intent.RoomOrContainer.Has<ContainerContents>())
            ItemHelpers.TryGetItemFromContainer(intent.Entity, intent.RoomOrContainer, intent.Item, out _);
        else
            return; // invalid container

        _msg.To(intent.Entity).Send($"You get {intent.Item.DisplayName} from {intent.RoomOrContainer.DisplayName}.");

        // event
        ref var itemGotEvt = ref _itemGotEvents.Add();
        itemGotEvt.Entity = intent.Entity;
        itemGotEvt.Item = intent.Item;
        itemGotEvt.RoomOrContainer = intent.RoomOrContainer;
    }

    private void HandleDrop(GameState state, DropItemIntent intent)
    {
        // TODO: validation

        // drop item from entity's inventory and put it in room
        ItemHelpers.TryDropItem(intent.Entity, intent.Room, intent.Item, out _);

        _msg.To(intent.Entity).Send($"You get {intent.Item.DisplayName} from {intent.Room.DisplayName}.");

        // event
        ref var itemDroppedEvt = ref _itemDroppedEvents.Add();
        itemDroppedEvt.Entity = intent.Entity;
        itemDroppedEvt.Item = intent.Item;
        itemDroppedEvt.Room = intent.Room;
    }

    private void HandleGive(GameState state, GiveItemIntent intent)
    {
        // TODO: validation

        // give item from entity to target
        ItemHelpers.TryGiveItem(intent.Entity, intent.Target, intent.Item, out _);

        _msg.To(intent.Entity).Send($"You give {intent.Item.DisplayName} to {intent.Target.DisplayName}.");

        // event
        ref var itemGivenEvt = ref _itemGivenEvents.Add();
        itemGivenEvt.Entity = intent.Entity;
        itemGivenEvt.Item = intent.Item;
        itemGivenEvt.Target = intent.Target;
    }

    private void HandlePut(GameState state, PutItemIntent intent)
    {
        // TODO: validation

        // put item from entity to container
        ItemHelpers.TryPutItem(intent.Entity, intent.Container, intent.Item, out _);

        _msg.To(intent.Entity).Send($"You put {intent.Item.DisplayName} in {intent.Container.DisplayName}.");

        // event
        ref var itemPutEvt = ref _itemPutEvents.Add();
        itemPutEvt.Entity = intent.Entity;
        itemPutEvt.Item = intent.Item;
        itemPutEvt.Container = intent.Container;
    }
}