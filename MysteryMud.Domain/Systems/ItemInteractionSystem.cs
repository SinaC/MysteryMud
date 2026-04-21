using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Bus;
using MysteryMud.Core.Contracts;
using MysteryMud.Core.Persistence;
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
    private readonly IDirtyTracker _dirtyTracker;
    private readonly IIntentContainer _intentContainer;
    private readonly IEventBuffer<ItemGotEvent> _itemGotEvents;
    private readonly IEventBuffer<ItemDroppedEvent> _itemDroppedEvents;
    private readonly IEventBuffer<ItemGivenEvent> _itemGivenEvents;
    private readonly IEventBuffer<ItemPutEvent> _itemPutEvents;
    private readonly IEventBuffer<ItemWornEvent> _itemWornEvents;
    private readonly IEventBuffer<ItemRemovedEvent> _itemRemovedEvents;
    private readonly IEventBuffer<ItemDestroyedEvent> _itemDestroyedEvents;
    private readonly IEventBuffer<ItemSacrificiedEvent> _itemSacrificedEvents;

    public ItemInteractionSystem(IGameMessageService gameMessageService, ISacrificeService sacrificeService, IDirtyTracker dirtyTracker, IIntentContainer intentContainer, IEventBuffer<ItemGotEvent> itemGotEvents, IEventBuffer<ItemDroppedEvent> itemDroppedEvents, IEventBuffer<ItemGivenEvent> itemGivenEvents, IEventBuffer<ItemPutEvent> itemPutEvents, IEventBuffer<ItemWornEvent> itemWornEvents, IEventBuffer<ItemRemovedEvent> itemRemovedEvents, IEventBuffer<ItemDestroyedEvent> itemDestroyedEvents, IEventBuffer<ItemSacrificiedEvent> itemSacrificedEvents)
    {
        _msg = gameMessageService;
        _sacrificeService = sacrificeService;
        _dirtyTracker = dirtyTracker;
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
        var entity = getItemIntent.Entity;
        var item = getItemIntent.Item;
        var sourceKind = getItemIntent.SourceKind;
        var source = getItemIntent.Source;

        // get item from room/container and put it in entity's inventory
        if (sourceKind == GetSourceKind.Room && source.Has<RoomContents>())
        {
            ItemHelpers.TryGetItemFromRoom(entity, source, item, out _);

            _msg.To(entity).Send($"You get {item.DisplayName}.");
        }
        else if (sourceKind == GetSourceKind.Container && source.Has<ContainerContents>())
        {
            ItemHelpers.TryGetItemFromContainer(entity, source, item, out _);

            _msg.To(entity).Send($"You get {item.DisplayName} from {source.DisplayName}.");
        }
        else
            return; // invalid source, should not happen if validation is done correctly

        _dirtyTracker.MarkDirty(entity, DirtyReason.ItemGained);

        // event
        ref var itemGotEvt = ref _itemGotEvents.Add();
        itemGotEvt.Entity = entity;
        itemGotEvt.Item = item;
        itemGotEvt.SourceKind = sourceKind;
        itemGotEvt.Source = source;
    }

    private void HandleDrop(GameState state, DropItemIntent dropItemIntent)
    {
        // TODO: validation
        var entity = dropItemIntent.Entity;
        var item = dropItemIntent.Item;
        var room = dropItemIntent.Room;

        // Unequip if necessary
        ref var equipped = ref item.TryGetRef<Equipped>(out var isEquipped);
        if (isEquipped)
        {
            ItemHelpers.TryUnequipItem(entity, equipped.Slot, out _);
        }

        // drop item from entity's inventory and put it in room
        ItemHelpers.TryDropItem(entity, room, item, out _);

        _msg.To(entity).Send($"You drop {item.DisplayName}.");

        _dirtyTracker.MarkDirty(entity, DirtyReason.ItemLost);

        // event
        ref var itemDroppedEvt = ref _itemDroppedEvents.Add();
        itemDroppedEvt.Entity = entity;
        itemDroppedEvt.Item = item;
        itemDroppedEvt.Room = room;
    }

    private void HandleGive(GameState state, GiveItemIntent giveItemIntent)
    {
        // TODO: validation
        var entity = giveItemIntent.Entity;
        var item = giveItemIntent.Item;
        var target = giveItemIntent.Target;

        // Unequip if necessary
        ref var equipped = ref item.TryGetRef<Equipped>(out var isEquipped);
        if (isEquipped)
        {
            ItemHelpers.TryUnequipItem(entity, equipped.Slot, out _);

            _dirtyTracker.MarkDirty(entity, DirtyReason.ItemRemoved);
        }

        // give item from entity to target
        ItemHelpers.TryGiveItem(entity, target, item, out _);

        _msg.To(entity).Send($"You give {item.DisplayName} to {target.DisplayName}.");

        _dirtyTracker.MarkDirty(entity, DirtyReason.ItemLost);
        _dirtyTracker.MarkDirty(target, DirtyReason.ItemGained);

        // event
        ref var itemGivenEvt = ref _itemGivenEvents.Add();
        itemGivenEvt.Entity = entity;
        itemGivenEvt.Item = item;
        itemGivenEvt.Target = target;
    }

    private void HandlePut(GameState state, PutItemIntent putItemIntent)
    {
        // TODO: validation
        var entity = putItemIntent.Entity;
        var item = putItemIntent.Item;
        var container = putItemIntent.Container;

        // Unequip if necessary
        ref var equipped = ref item.TryGetRef<Equipped>(out var isEquipped);
        if (isEquipped)
        {
            ItemHelpers.TryUnequipItem(entity, equipped.Slot, out _);

            _dirtyTracker.MarkDirty(entity, DirtyReason.ItemRemoved);
        }

        // put item from entity to container
        ItemHelpers.TryPutItem(entity, container, item, out _);

        _msg.To(entity).Send($"You put {item.DisplayName} in {container.DisplayName}.");

        _dirtyTracker.MarkDirty(entity, DirtyReason.ItemLost);

        // event
        ref var itemPutEvt = ref _itemPutEvents.Add();
        itemPutEvt.Entity = entity;
        itemPutEvt.Item = item;
        itemPutEvt.Container = container;
    }

    private void HandleWear(GameState state, WearItemIntent wearItemIntent)
    {
        // TODO: validation
        var entity = wearItemIntent.Entity;
        var item = wearItemIntent.Item;
        var slot = wearItemIntent.Slot;

        ItemHelpers.TryEquipItem(entity, item, out _);

        _msg.To(entity).Send($"You wear {item.DisplayName}.");

        _dirtyTracker.MarkDirty(entity, DirtyReason.ItemEquipped);

        // event
        ref var itemWornEvt = ref _itemWornEvents.Add();
        itemWornEvt.Entity = entity;
        itemWornEvt.Item = item;
        itemWornEvt.Slot = slot;
    }

    private void HandleRemove(GameState state, RemoveItemIntent removeItemIntent)
    {
        // TODO: validation
        var entity = removeItemIntent.Entity;
        var item = removeItemIntent.Item;
        var slot = removeItemIntent.Slot;

        ItemHelpers.TryUnequipItem(entity, slot, out _);

        _msg.To(entity).Send($"You remove {item.DisplayName}.");

        _dirtyTracker.MarkDirty(entity, DirtyReason.ItemRemoved);

        // event
        ref var itemRemovedEvt = ref _itemRemovedEvents.Add();
        itemRemovedEvt.Entity = entity;
        itemRemovedEvt.Item = item;
        itemRemovedEvt.Slot = slot;
    }

    private void HandleDestroy(GameState state, DestroyItemIntent destroyItemIntent)
    {
        // TODO: validation
        var entity = destroyItemIntent.Entity;
        var item = destroyItemIntent.Item;


        // Unequip if necessary
        ref var equipped = ref item.TryGetRef<Equipped>(out var isEquipped);
        if (isEquipped)
        {
            ItemHelpers.TryUnequipItem(entity, equipped.Slot, out _);

            _dirtyTracker.MarkDirty(entity, DirtyReason.ItemRemoved);
        }

        DestroyItem(item);

        _msg.To(entity).Send($"You destroy {item.DisplayName}.");

        // event
        ref var itemDestroyedEvt = ref _itemDestroyedEvents.Add();
        itemDestroyedEvt.Entity = entity;
        itemDestroyedEvt.Item = item;
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