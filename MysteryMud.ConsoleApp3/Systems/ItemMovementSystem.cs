using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Components.Rooms;

namespace MysteryMud.ConsoleApp3.Systems;

class ItemMovementSystem
{
    public static void GetItemFromRoom(Entity getter, Entity room, Entity item)
    {
        ref var roomContents = ref room.Get<RoomContents>();
        ref var inventory = ref getter.Get<Inventory>();

        roomContents.Items.Remove(item);
        inventory.Items.Add(item);
        item.Remove<Location>();
        item.Add(new ContainedIn { Character = getter });
    }

    public static void GetItemFromContainer(Entity getter, Entity container, Entity item)
    {
        ref var containerContents = ref container.Get<ContainerContents>();
        ref var inventory = ref getter.Get<Inventory>();
        ref var containedIn = ref item.Get<ContainedIn>();

        containerContents.Items.Remove(item);
        inventory.Items.Add(item);
        containedIn.Container = Entity.Null;
        containedIn.Character = getter;
    }

    public static void DropItem(Entity dropped, Entity room, Entity item)
    {
        ref var roomContents = ref room.Get<RoomContents>();
        ref var inventory = ref dropped.Get<Inventory>();

        roomContents.Items.Add(item);
        inventory.Items.Remove(item);
        item.Add(new Location { Room = room });
        item.Remove<ContainedIn>();
    }

    public static void GiveItem(Entity giver, Entity receiver, Entity item)
    {
        ref var giverInventory = ref giver.Get<Inventory>();
        ref var receiverInventory = ref receiver.Get<Inventory>();
        ref var containedIn = ref item.Get<ContainedIn>();

        giverInventory.Items.Remove(item);
        receiverInventory.Items.Add(item);
        containedIn.Character = receiver;
    }

    public static void PutItem(Entity actor, Entity container, Entity item)
    {
        ref var containerContents = ref container.Get<ContainerContents>();
        ref var inventory = ref actor.Get<Inventory>();
        ref var containedIn = ref item.Get<ContainedIn>();

        containerContents.Items.Add(item);
        inventory.Items.Remove(item);
        containedIn.Character = Entity.Null;
        containedIn.Container = container;
    }
}
