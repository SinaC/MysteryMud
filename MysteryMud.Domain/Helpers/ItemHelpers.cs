using Arch.Core;
using Arch.Core.Extensions;
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
        return entities.All(x => x.IsAlive() && !x.Has<DestroyedTag>());
    }

    // TODO: These methods is currently very basic and does not handle edge cases such as weight limits, item ownership, or other game mechanics. It should be expanded to include these features as needed.
    public static bool TryGetItemFromRoom(Entity getter, Entity room, Entity item, out string? reason)
    {
        reason = null;

        ref var roomContents = ref room.Get<RoomContents>();
        ref var inventory = ref getter.Get<Inventory>();

        roomContents.Items.Remove(item);
        inventory.Items.Add(item);
        item.Remove<Location>();
        item.Add(new ContainedIn { Character = getter });

        return true;
    }

    public static bool TryGetItemFromContainer(Entity getter, Entity container, Entity item, out string? reason)
    {
        reason = null;

        ref var containerContents = ref container.Get<ContainerContents>();
        ref var inventory = ref getter.Get<Inventory>();
        ref var containedIn = ref item.Get<ContainedIn>();

        containerContents.Items.Remove(item);
        inventory.Items.Add(item);
        containedIn.Container = Entity.Null;
        containedIn.Character = getter;

        return true;
    }

    public static bool TryDropItem(Entity dropped, Entity room, Entity item, out string? reason)
    {
        reason = null;

        ref var roomContents = ref room.Get<RoomContents>();
        ref var inventory = ref dropped.Get<Inventory>();

        roomContents.Items.Add(item);
        inventory.Items.Remove(item);
        item.Add(new Location { Room = room });
        item.Remove<ContainedIn>();

        return true;
    }

    public static bool TryGiveItem(Entity giver, Entity receiver, Entity item, out string? reason)
    {
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
        reason = null;

        ref var containerContents = ref container.Get<ContainerContents>();
        ref var inventory = ref actor.Get<Inventory>();
        ref var containedIn = ref item.Get<ContainedIn>();

        containerContents.Items.Add(item);
        inventory.Items.Remove(item);
        containedIn.Character = Entity.Null;
        containedIn.Container = container;

        return true;
    }

    public static bool TryEquipItem(Entity actor, Entity item, out string? reason)
    {
        reason = null;

        ref var equipable = ref item.Get<Equipable>();
        ref var equipment = ref actor.Get<Equipment>();

        var slot = equipable.Slot;

        if (equipment.Slots.ContainsKey(slot))
            return false;

        equipment.Slots[slot] = item;

        item.Add(new Equipped
        {
            Wearer = actor,
            Slot = slot
        });

        actor.Add<DirtyStats>();

        return true;
    }

    public static void TryUnequipItem(Entity actor, EquipmentSlotKind slot, out string? reason)
    {
        reason = null;

        ref var equipment = ref actor.Get<Equipment>();

        if (!equipment.Slots.TryGetValue(slot, out var item))
            return;

        equipment.Slots.Remove(slot);

        item.Remove<Equipped>();

        actor.Add<DirtyStats>();
    }
}
