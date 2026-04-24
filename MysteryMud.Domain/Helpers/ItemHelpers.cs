using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Helpers;

public static class ItemHelpers
{
    public static bool IsAlive(World world, params EntityId[] entities)
    {
        return entities.All(x => world.IsAlive(x) && !world.Has<DestroyedTag>(x));
    }

    // TODO: These methods are currently very basic and does not handle edge cases such as weight limits, item ownership, or other game mechanics. It should be expanded to include these features as needed.
    public static bool TryGetItemFromRoom(World world, EntityId getter, EntityId room, EntityId item, out string? reason)
    {
        if (!IsAlive(world, item))
        {
            reason = "destroyed";
            return false;
        }
        if (!world.Has<Location>(item) || world.Has<ContainedIn>(item))
        {
            reason = "invalid state";
            return false;
        }


        ref var roomContents = ref world.Get<RoomContents>(room);
        ref var inventory = ref world.Get<Inventory>(getter);

        roomContents.Items.Remove(item);
        inventory.Items.Add(item);
        world.Remove<Location>(item);
        world.Add(item, new ContainedIn { Character = getter });

        reason = null;
        return true;
    }

    public static bool TryGetItemFromContainer(World world, EntityId getter, EntityId container, EntityId item, out string? reason)
    {
        if (!IsAlive(world, item))
        {
            reason = "destroyed";
            return false;
        }


        ref var itemOwner = ref world.TryGetRef<ItemOwner>(item, out var hasOwner);
        if (hasOwner && getter != itemOwner.Owner)
        {
            reason = "not the owner";
            return false;
        }

        ref var containerContents = ref world.Get<ContainerContents>(container);
        ref var inventory = ref world.Get<Inventory>(getter);
        ref var containedIn = ref world.Get<ContainedIn>(item);

        containerContents.Items.Remove(item);
        inventory.Items.Add(item);
        containedIn.Container = EntityId.Invalid;
        containedIn.Character = getter;

        reason = null;
        return true;
    }

    public static bool TryDropItem(World world, EntityId dropper, EntityId room, EntityId item, out string? reason)
    {
        if (!IsAlive(world, item))
        {
            reason = "destroyed";
            return false;
        }

        if (world.Has<Location>(item) || !world.Has<ContainedIn>(item))
        {
            reason = "invalid state";
            return false;
        }

        reason = null;

        ref var roomContents = ref world.Get<RoomContents>(room);
        ref var inventory = ref world.Get<Inventory>(dropper);

        roomContents.Items.Add(item);
        inventory.Items.Remove(item);

        world.Add(item, new Location { Room = room });
        world.Remove<ContainedIn>(item);

        return true;
    }

    public static bool TryGiveItem(World world, EntityId giver, EntityId receiver, EntityId item, out string? reason)
    {
        if (!IsAlive(world, item))
        {
            reason = "destroyed";
            return false;
        }

        reason = null;

        ref var giverInventory = ref world.Get<Inventory>(giver);
        ref var receiverInventory = ref world.Get<Inventory>(receiver);
        ref var containedIn = ref world.Get<ContainedIn>(item);

        giverInventory.Items.Remove(item);
        receiverInventory.Items.Add(item);
        containedIn.Character = receiver;

        return true;
    }

    public static bool TryPutItem(World world, EntityId putter, EntityId container, EntityId item, out string? reason)
    {
        if (!IsAlive(world, item))
        {
            reason = "destroyed";
            return false;
        }

        reason = null;

        ref var containerContents = ref world.Get<ContainerContents>(container);
        ref var inventory = ref world.Get<Inventory>(putter);
        ref var containedIn = ref world.Get<ContainedIn>(item);

        containerContents.Items.Add(item);
        inventory.Items.Remove(item);
        containedIn.Character = EntityId.Invalid;
        containedIn.Container = container;

        return true;
    }

    public static bool TryEquipItem(World world, EntityId equipper, EntityId item, out string? reason)
    {
        if (!IsAlive(world, item))
        {
            reason = "destroyed";
            return false;
        }
        if (world.Has<Equipped>(item))
        {
            reason = "invalid state";
            return false;
        }

        ref var equipable = ref world.Get<Equipable>(item);
        ref var equipment = ref world.Get<Equipment>(equipper);

        var slot = equipable.Slot;

        if (equipment.Slots.ContainsKey(slot))
        {
            reason = "inexising slot";
            return false;
        }

        equipment.Slots[slot] = item;

        world.Add(item, new Equipped
        {
            Wearer = equipper,
            Slot = slot
        });

        if (!world.Has<DirtyStats>(equipper))
            world.Add<DirtyStats>(equipper);

        reason = null;
        return true;
    }

    public static bool TryUnequipItem(World world, EntityId unequipper, EquipmentSlotKind slot, out string? reason)
    {
        ref var equipment = ref world.Get<Equipment>(unequipper);

        if (!equipment.Slots.TryGetValue(slot, out var item))
        {
            reason = "nothing equipped on that slot";
            return false;
        }

        equipment.Slots.Remove(slot);

        if (!IsAlive(world, item))
        {
            reason = "destroyed";
            return false;
        }

        if (world.Has<Equipped>(item))
            world.Remove<Equipped>(item);

        if (!world.Has<DirtyStats>(unequipper))
            world.Add<DirtyStats>(unequipper);

        reason = null;
        return true;
    }

    public static void DestroyItem(World world, EntityId item)
    {
        if (!IsAlive(world, item))
            return;

        if (!world.Has<DestroyedTag>(item))
            world.Add<DestroyedTag>(item);

        if (world.Has<Container>(item))
        {
            var container = world.Get<ContainerContents>(item);
            foreach (var containedItem in container.Items)
            {
                DestroyItem(world, containedItem); // recursive call to destroy contained items
            }
        }
    }
}
