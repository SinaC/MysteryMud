using DefaultEcs;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Helpers;

public static class ItemHelpers
{
    public static bool IsAlive(params Entity[] entities)
    {
        return entities.All(x => x.IsAlive && !x.Has<DestroyedTag>());
    }

    // TODO: These methods are currently very basic and does not handle edge cases such as weight limits, item ownership, or other game mechanics. It should be expanded to include these features as needed.
    public static bool TryGetItemFromRoom(Entity getter, Entity room, Entity item, out string? reason)
    {
        if (!IsAlive(item))
        {
            reason = "destroyed";
            return false;
        }
        if (!item.Has<Location>() || item.Has<ContainedIn>())
        {
            reason = "invalid state";
            return false;
        }


        ref var roomContents = ref room.Get<RoomContents>();
        ref var inventory = ref getter.Get<Inventory>();

        roomContents.Items.Remove(item);
        inventory.Items.Add(item);
        item.Remove<Location>();
        item.Set(new ContainedIn { Character = getter });

        reason = null;
        return true;
    }

    public static bool TryGetItemFromContainer(Entity getter, Entity container, Entity item, out string? reason)
    {
        if (!IsAlive(item))
        {
            reason = "destroyed";
            return false;
        }

        if (item.Has<ItemOwner>() && item.Get<ItemOwner>().Owner != getter)
        {
            reason = "not the owner";
            return false;
        }

        ref var containerContents = ref container.Get<ContainerContents>();
        ref var inventory = ref getter.Get<Inventory>();
        ref var containedIn = ref item.Get<ContainedIn>();

        containerContents.Items.Remove(item);
        inventory.Items.Add(item);
        containedIn.Container = default;
        containedIn.Character = getter;

        reason = null;
        return true;
    }

    public static bool TryDropItem(Entity dropped, Entity room, Entity item, out string? reason)
    {
        if (!IsAlive(item))
        {
            reason = "destroyed";
            return false;
        }

        if (item.Has<Location>() || !item.Has<ContainedIn>())
        {
            reason = "invalid state";
            return false;
        }

        reason = null;

        ref var roomContents = ref room.Get<RoomContents>();
        ref var inventory = ref dropped.Get<Inventory>();

        roomContents.Items.Add(item);
        inventory.Items.Remove(item);

        item.Set(new Location { Room = room });
        item.Remove<ContainedIn>();

        return true;
    }

    public static bool TryGiveItem(Entity giver, Entity receiver, Entity item, out string? reason)
    {
        if (!IsAlive(item))
        {
            reason = "destroyed";
            return false;
        }

        reason = null;

        ref var giverInventory = ref giver.Get<Inventory>();
        ref var receiverInventory = ref receiver.Get<Inventory>();
        ref var containedIn = ref item.Get<ContainedIn>();

        giverInventory.Items.Remove(item);
        receiverInventory.Items.Add(item);
        containedIn.Character = receiver;

        return true;
    }

    public static bool TryPutItem(Entity actor, Entity container, Entity item, out string? reason)
    {
        if (!IsAlive(item))
        {
            reason = "destroyed";
            return false;
        }

        reason = null;

        ref var containerContents = ref container.Get<ContainerContents>();
        ref var inventory = ref actor.Get<Inventory>();
        ref var containedIn = ref item.Get<ContainedIn>();

        containerContents.Items.Add(item);
        inventory.Items.Remove(item);
        containedIn.Character = default;
        containedIn.Container = container;

        return true;
    }

    public static bool TryEquipItem(Entity actor, Entity item, out string? reason)
    {
        if (!IsAlive(item))
        {
            reason = "destroyed";
            return false;
        }
        if (item.Has<Equipped>())
        {
            reason = "invalid state";
            return false;
        }

        ref var equipable = ref item.Get<Equipable>();
        ref var equipment = ref actor.Get<Equipment>();

        var slot = equipable.Slot;

        if (equipment.Slots.ContainsKey(slot))
        {
            reason = "inexising slot";
            return false;
        }

        equipment.Slots[slot] = item;

        item.Set(new Equipped
        {
            Wearer = actor,
            Slot = slot
        });

        if (!actor.Has<DirtyStats>())
            actor.Set<DirtyStats>();

        reason = null;
        return true;
    }

    public static bool TryUnequipItem(Entity actor, EquipmentSlotKind slot, out string? reason)
    {
        ref var equipment = ref actor.Get<Equipment>();

        if (!equipment.Slots.TryGetValue(slot, out var item))
        {
            reason = "nothing equipped on that slot";
            return false;
        }

        equipment.Slots.Remove(slot);

        if (!IsAlive(item))
        {
            reason = "destroyed";
            return false;
        }

        if (item.Has<Equipped>())
            item.Remove<Equipped>();

        if (!actor.Has<DirtyStats>())
            actor.Set<DirtyStats>();

        reason = null;
        return true;
    }

    public static void DestroyItem(Entity item)
    {
        if (!IsAlive(item))
            return;

        if (!item.Has<DestroyedTag>())
            item.Set<DestroyedTag>();

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
