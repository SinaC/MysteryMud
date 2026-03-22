using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Characters.Mobiles;
using MysteryMud.ConsoleApp3.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Data.Enums;

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
            new RoomGraph { Exits = [] },
            new RoomContents
            {
                Characters = [],
                Items = []
            },
            new RoomNeighborhood
            {
                Distance1 = [],
                Distance2 = []
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

    public static Entity CreateConnectingPlayer(World world, int connectionId)
    {
        var player = world.Create(new Connection
        {
            ConnectionId = connectionId
        });

        // Name, Location, ... will be set in nanny

        return player;
    }

    public static Entity CreatePlayer(World world, string name, Entity room)
    {
        var player = world.Create(
            new PlayerTag(),
            new Name { Value = name },
            new BaseStats
            {
                Level = 1,
                Experience = 0,
                Values = new Dictionary<StatType, int>
                {
                    [StatType.Strength] = 15,
                    [StatType.Intelligence] = 10,
                    [StatType.Wisdom] = 15,
                    [StatType.Dexterity] = 12,
                    [StatType.Constitution] = 15,
                    [StatType.HitRoll] = 0,
                    [StatType.DamRoll] = 0,
                    [StatType.Armor] = 0
                }
            },
            new EffectiveStats
            {
                Level = 1,
                Experience = 0,
                Values = new Dictionary<StatType, int>
                {
                    [StatType.Strength] = 15,
                    [StatType.Intelligence] = 10,
                    [StatType.Wisdom] = 15,
                    [StatType.Dexterity] = 12,
                    [StatType.Constitution] = 15,
                    [StatType.HitRoll] = 0,
                    [StatType.DamRoll] = 0,
                    [StatType.Armor] = 0
                }
            },
            new Health { Current = 10, Max = 100 },
            new Inventory { Items = [] },
            new Equipment { Slots = [] },
            new CharacterEffects
            {
                Effects = [],
                EffectsByTag = new Entity?[32]
            },
            new PositionComponent { Position = Position.Standing },
            new Location { Room = room },
            new DirtyStats() // dirty by default
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
            new BaseStats
            {
                Level = 1,
                Experience = 0,
                Values = new Dictionary<StatType, int>
                {
                    [StatType.Strength] = 15,
                    [StatType.Intelligence] = 10,
                    [StatType.Wisdom] = 15,
                    [StatType.Dexterity] = 12,
                    [StatType.Constitution] = 15,
                    [StatType.HitRoll] = 0,
                    [StatType.DamRoll] = 0,
                    [StatType.Armor] = 0
                }
            },
            new EffectiveStats
            {
                Level = 1,
                Experience = 0,
                Values = new Dictionary<StatType, int>
                {
                    [StatType.Strength] = 15,
                    [StatType.Intelligence] = 10,
                    [StatType.Wisdom] = 15,
                    [StatType.Dexterity] = 12,
                    [StatType.Constitution] = 15,
                    [StatType.HitRoll] = 0,
                    [StatType.DamRoll] = 0,
                    [StatType.Armor] = 0
                }
            },
            new Health { Current = 5, Max = 100 },
            new Inventory { Items = [] },
            new Equipment { Slots = [] },
            new CharacterEffects
            {
                Effects = [],
                EffectsByTag = new Entity?[32]
            },
            new Location { Room = room },
            new PositionComponent { Position = Position.Standing },
            new ThreatTable { Threat = [] },
            new DirtyStats() // dirty by default
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
