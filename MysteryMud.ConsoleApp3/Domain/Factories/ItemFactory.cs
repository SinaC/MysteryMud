using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Domain.Components;
using MysteryMud.ConsoleApp3.Domain.Components.Characters;
using MysteryMud.ConsoleApp3.Domain.Components.Items;
using MysteryMud.ConsoleApp3.Domain.Components.Rooms;

namespace MysteryMud.ConsoleApp3.Domain.Factories;

class ItemFactory
{
    public static Entity CreateItemInRoom(World world, string name, string description, Entity room)
    {
        var item = world.Create(
            new Item(),
            new Name { Value = name },
            new Description { Value = description },
            new Location { Room = room }
        );
        // TODO: check that room has RoomContents component
        room.Get<RoomContents>().Items.Add(item);
        return item;
    }

    public static Entity CreateItemInInventory(World world, string name, string description, Entity character)
    {
        var item = world.Create(
            new Item(),
            new Name { Value = name },
            new Description { Value = description },
            new ContainedIn { Character = character }
        );
        // TODO: check that character has Inventory component
        character.Get<Inventory>().Items.Add(item);
        return item;
    }

    internal static Entity CreateItemInContainer(World world, string name, string description, Entity container)
    {
        var item = world.Create(
            new Item(),
            new Name { Value = name },
            new Description { Value = description },
            new ContainedIn { Container = container }
        );
        // TODO: check that container has ContainerContents component
        container.Get<ContainerContents>().Items.Add(item);
        return item;
    }
}
