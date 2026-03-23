using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Characters.Players;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Factories;

class PlayerFactory
{
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
}
