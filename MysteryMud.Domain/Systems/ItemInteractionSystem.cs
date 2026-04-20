using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;
using MysteryMud.GameData.Intents;

namespace MysteryMud.Domain.Systems;

public class ItemInteractionSystem
{
    private readonly IGameMessageService _msg;
    private readonly ISacrificeService _sacrificeService;
    private readonly IIntentContainer _intentContainer;
    private readonly IEventBuffer<ItemGotEvent> _itemGotEvents;
    private readonly IEventBuffer<ItemDroppedEvent> _itemDroppedEvents;
    private readonly IEventBuffer<ItemGivenEvent> _itemGivenEvents;
    private readonly IEventBuffer<ItemPutEvent> _itemPutEvents;
    private readonly IEventBuffer<ItemWornEvent> _itemWornEvents;
    private readonly IEventBuffer<ItemRemovedEvent> _itemRemovedEvents;
    private readonly IEventBuffer<ItemDestroyedEvent> _itemDestroyedEvents;
    private readonly IEventBuffer<ItemSacrificiedEvent> _itemSacrificedEvents;

    public ItemInteractionSystem(IGameMessageService gameMessageService, ISacrificeService sacrificeService, IIntentContainer intentContainer, IEventBuffer<ItemGotEvent> itemGotEvents, IEventBuffer<ItemDroppedEvent> itemDroppedEvents, IEventBuffer<ItemGivenEvent> itemGivenEvents, IEventBuffer<ItemPutEvent> itemPutEvents, IEventBuffer<ItemWornEvent> itemWornEvents, IEventBuffer<ItemRemovedEvent> itemRemovedEvents, IEventBuffer<ItemDestroyedEvent> itemDestroyedEvents, IEventBuffer<ItemSacrificiedEvent> itemSacrificedEvents)
    {
        _msg = gameMessageService;
        _sacrificeService = sacrificeService;
        _intentContainer = intentContainer;
        _itemGotEvents = itemGotEvents;
        _itemDroppedEvents = itemDroppedEvents;
        _itemGivenEvents = itemGivenEvents;
        _itemPutEvents = itemPutEvents;
        _itemWornEvents = itemWornEvents;
        _itemRemovedEvents = itemRemovedEvents;
        _itemDestroyedEvents = itemDestroyedEvents;
        _itemSacrificedEvents = itemSacrificedEvents;
    }

    public void Tick(GameState state)
    {
        // handle get/drop/give/put/wear/remove/destroy/sacrifice intents
        foreach (ref var getIntent in _intentContainer.GetItemSpan)
            HandleGet(state, getIntent);
        foreach (ref var dropIntent in _intentContainer.DropItemSpan)
            HandleDrop(state, dropIntent);
        foreach (ref var giveIntent in _intentContainer.GiveItemSpan)
            HandleGive(state, giveIntent);
        foreach (ref var putIntent in _intentContainer.PutItemSpan)
            HandlePut(state, putIntent);
        foreach(ref var wearIntent in _intentContainer.WearItemSpan)
            HandleWear(state, wearIntent);
        foreach(ref var removeIntent in _intentContainer.RemoveItemSpan)
            HandleRemove(state, removeIntent);
        foreach(ref var destroyIntent in _intentContainer.DestroyItemSpan)
            HandleDestroy(state, destroyIntent);
        foreach (ref var sacrificeIntent in _intentContainer.SacrificeItemSpan)
            HandleSacrifice(state, sacrificeIntent);
    }

    private void HandleGet(GameState state, GetItemIntent getItemIntent)
    {
        // TODO: validation

        // get item from room/container and put it in entity's inventory
        if (getItemIntent.SourceKind == GetSourceKind.Room && getItemIntent.Source.Has<RoomContents>())
        {
            ItemHelpers.TryGetItemFromRoom(getItemIntent.Entity, getItemIntent.Source, getItemIntent.Item, out _);

            _msg.To(getItemIntent.Entity).Send($"You get {getItemIntent.Item.DisplayName}.");
        }
        else if (getItemIntent.SourceKind == GetSourceKind.Container && getItemIntent.Source.Has<ContainerContents>())
        {
            ItemHelpers.TryGetItemFromContainer(getItemIntent.Entity, getItemIntent.Source, getItemIntent.Item, out _);

            _msg.To(getItemIntent.Entity).Send($"You get {getItemIntent.Item.DisplayName} from {getItemIntent.Source.DisplayName}.");
        }
        else
            return; // invalid source, should not happen if validation is done correctly

        // event
        ref var itemGotEvt = ref _itemGotEvents.Add();
        itemGotEvt.Entity = getItemIntent.Entity;
        itemGotEvt.Item = getItemIntent.Item;
        itemGotEvt.SourceKind = getItemIntent.SourceKind;
        itemGotEvt.Source = getItemIntent.Source;
    }

    private void HandleDrop(GameState state, DropItemIntent dropItemIntent)
    {
        // TODO: validation

        // Unequip if necessary
        ref var equipped = ref dropItemIntent.Item.TryGetRef<Equipped>(out var isEquipped);
        if (isEquipped)
        {
            ItemHelpers.TryUnequipItem(dropItemIntent.Entity, equipped.Slot, out _);
        }

        // drop item from entity's inventory and put it in room
        ItemHelpers.TryDropItem(dropItemIntent.Entity, dropItemIntent.Room, dropItemIntent.Item, out _);

        _msg.To(dropItemIntent.Entity).Send($"You drop {dropItemIntent.Item.DisplayName}.");

        // event
        ref var itemDroppedEvt = ref _itemDroppedEvents.Add();
        itemDroppedEvt.Entity = dropItemIntent.Entity;
        itemDroppedEvt.Item = dropItemIntent.Item;
        itemDroppedEvt.Room = dropItemIntent.Room;
    }

    private void HandleGive(GameState state, GiveItemIntent giveItemIntent)
    {
        // TODO: validation

        // Unequip if necessary
        ref var equipped = ref giveItemIntent.Item.TryGetRef<Equipped>(out var isEquipped);
        if (isEquipped)
        {
            ItemHelpers.TryUnequipItem(giveItemIntent.Entity, equipped.Slot, out _);
        }

        // give item from entity to target
        ItemHelpers.TryGiveItem(giveItemIntent.Entity, giveItemIntent.Target, giveItemIntent.Item, out _);

        _msg.To(giveItemIntent.Entity).Send($"You give {giveItemIntent.Item.DisplayName} to {giveItemIntent.Target.DisplayName}.");

        // event
        ref var itemGivenEvt = ref _itemGivenEvents.Add();
        itemGivenEvt.Entity = giveItemIntent.Entity;
        itemGivenEvt.Item = giveItemIntent.Item;
        itemGivenEvt.Target = giveItemIntent.Target;
    }

    private void HandlePut(GameState state, PutItemIntent putItemIntent)
    {
        // TODO: validation

        // Unequip if necessary
        ref var equipped = ref putItemIntent.Item.TryGetRef<Equipped>(out var isEquipped);
        if (isEquipped)
        {
            ItemHelpers.TryUnequipItem(putItemIntent.Entity, equipped.Slot, out _);
        }

        // put item from entity to container
        ItemHelpers.TryPutItem(putItemIntent.Entity, putItemIntent.Container, putItemIntent.Item, out _);

        _msg.To(putItemIntent.Entity).Send($"You put {putItemIntent.Item.DisplayName} in {putItemIntent.Container.DisplayName}.");

        // event
        ref var itemPutEvt = ref _itemPutEvents.Add();
        itemPutEvt.Entity = putItemIntent.Entity;
        itemPutEvt.Item = putItemIntent.Item;
        itemPutEvt.Container = putItemIntent.Container;
    }

    private void HandleWear(GameState state, WearItemIntent wearItemIntent)
    {
        // TODO: validation

        ItemHelpers.TryEquipItem(wearItemIntent.Actor, wearItemIntent.Item, out _);

        _msg.To(wearItemIntent.Actor).Send($"You wear {wearItemIntent.Item.DisplayName}.");

        // event
        ref var itemWornEvt = ref _itemWornEvents.Add();
        itemWornEvt.Actor = wearItemIntent.Actor;
        itemWornEvt.Item = wearItemIntent.Item;
        itemWornEvt.Slot = wearItemIntent.Slot;
    }

    private void HandleRemove(GameState state, RemoveItemIntent removeItemIntent)
    {
        // TODO: validation

        ItemHelpers.TryUnequipItem(removeItemIntent.Actor, removeItemIntent.Slot, out _);

        _msg.To(removeItemIntent.Actor).Send($"You remove {removeItemIntent.Item.DisplayName}.");

        // event
        ref var itemRemovedEvt = ref _itemRemovedEvents.Add();
        itemRemovedEvt.Actor = removeItemIntent.Actor;
        itemRemovedEvt.Item = removeItemIntent.Item;
        itemRemovedEvt.Slot = removeItemIntent.Slot;
    }

    private void HandleDestroy(GameState state, DestroyItemIntent destroyItemIntent)
    {
        // TODO: validation

        // Unequip if necessary
        ref var equipped = ref destroyItemIntent.Item.TryGetRef<Equipped>(out var isEquipped);
        if (isEquipped)
        {
            ItemHelpers.TryUnequipItem(destroyItemIntent.Entity, equipped.Slot, out _);
        }

        DestroyItem(destroyItemIntent.Item);

        _msg.To(destroyItemIntent.Entity).Send($"You destroy {destroyItemIntent.Item.DisplayName}.");

        // event
        ref var itemDestroyedEvt = ref _itemDestroyedEvents.Add();
        itemDestroyedEvt.Entity = destroyItemIntent.Entity;
        itemDestroyedEvt.Item = destroyItemIntent.Item;
    }

    private void HandleSacrifice(GameState state, SacrificeItemIntent sacrificeItemIntent)
    {
        _sacrificeService.Sacrifice(sacrificeItemIntent.Entity, sacrificeItemIntent.Item);

        // event
        ref var itemDestroyedEvt = ref _itemSacrificedEvents.Add();
        itemDestroyedEvt.Entity = sacrificeItemIntent.Entity;
        itemDestroyedEvt.Item = sacrificeItemIntent.Item;
    }

    private static void DestroyItem(Entity item)
    {
        if (!item.Has<DestroyedTag>())
            item.Add<DestroyedTag>();

        if (item.Has<Container>())
        {
            var container = item.Get<ContainerContents>();
            foreach (var containedItem in container.Items)
            {
                DestroyItem(containedItem); // recursive call to destroy contained items
            }
        }
    }
}