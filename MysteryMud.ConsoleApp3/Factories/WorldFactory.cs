using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Enums;

namespace MysteryMud.ConsoleApp3.Factories;

class WorldFactory
{
    public static Entity StartingRoomEntity;
    public static Entity RespawnRoomEntity;

    public static Entity CreateRoom(World world, int id, string name, string description)
    {
        return world.Create(
            new Room { Id = id },
            new Name { Value = name },
            new Description { Value = description },
            new RoomGraph { Exits = new List<Exit>() },
            new RoomContents
            {
                Characters = new List<Entity>(),
                Items = new List<Entity>()
            },
            new RoomNeighborhood
            {
                Distance1 = new List<Entity>(),
                Distance2 = new List<Entity>()
            }
        );
    }

    public static bool LinkRoom(World world, Entity sourceRoom, Entity targetRoom, Direction direction)
    {
        var sourceRoomGraph = sourceRoom.Get<RoomGraph>();
        if (sourceRoomGraph.Exits.Any(x => x.Direction == direction))
        {
            return false; // Exit already exists in this direction
        }

        sourceRoomGraph.Exits.Add(new Exit { Direction = direction, TargetRoom = targetRoom }); // TODO: close + description
        return true;
    }

    public static Entity CreatePlayer(World world)
    {
        var player = world.Create(
            new PlayerTag(),
            new CharacterStats { Strength = 15, Dexterity = 12 },
            new EffectiveStats(),
            new Health { Current = 100, Max = 100 },
            new Inventory { Items = new List<Entity>() },
            new Equipment { Slots = new Dictionary<EquipmentSlot, Entity>() }
            // Name, Position, Connection will be set in nanny
        );

        return player;
    }

    public static Entity CreatePlayer(World world, string name, Entity room)
    {
        var player = world.Create(
            new PlayerTag(),
            new Name { Value = name },
            new CharacterStats { Strength = 15, Dexterity = 12 },
            new EffectiveStats(),
            new Health { Current = 10, Max = 100 },
            new Inventory { Items = new List<Entity>() },
            new Equipment { Slots = new Dictionary<EquipmentSlot, Entity>() },
            new Position { Room = room }
            //new CombatState()
        );

        room.Get<RoomContents>().Characters.Add(player);

        return player;
    }

    public static Entity CreateMob(World world, string name, string description, Entity room)
    {
        var player = world.Create(
            new NpcTag(),
            new Name { Value = name },
            new Description { Value = description },
            new CharacterStats { Strength = 15, Dexterity = 12 },
            new EffectiveStats(),
            new Health { Current = 5, Max = 100 },
            new Inventory { Items = new List<Entity>() },
            new Equipment { Slots = new Dictionary<EquipmentSlot, Entity>() },
            new Position { Room = room },
            //new CombatState(),
            new ThreatTable { Threat = new Dictionary<Entity, int>() }
        );

        room.Get<RoomContents>().Characters.Add(player);

        return player;
    }

    public static Entity CreateItemInRoom(World world, string name, string description, Entity room)
    {
        var item = world.Create(
            new Item(),
            new Name { Value = name },
            new Description { Value = description },
            new Position { Room = room }
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
