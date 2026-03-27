using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Factories;

public static class PlayerFactory
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
            new CharacterTag(),
            new PlayerTag(),
            new Name { Value = name },
            new BaseStats
            {
                Level = 1,
                Experience = 0,
                Values = new Dictionary<StatTypes, int>
                {
                    [StatTypes.Strength] = 15,
                    [StatTypes.Intelligence] = 10,
                    [StatTypes.Wisdom] = 15,
                    [StatTypes.Dexterity] = 12,
                    [StatTypes.Constitution] = 15,
                    [StatTypes.HitRoll] = 0,
                    [StatTypes.DamRoll] = 0,
                    [StatTypes.Armor] = 0
                }
            },
            new EffectiveStats
            {
                Level = 1,
                Experience = 0,
                Values = new Dictionary<StatTypes, int>
                {
                    [StatTypes.Strength] = 15,
                    [StatTypes.Intelligence] = 10,
                    [StatTypes.Wisdom] = 15,
                    [StatTypes.Dexterity] = 12,
                    [StatTypes.Constitution] = 15,
                    [StatTypes.HitRoll] = 0,
                    [StatTypes.DamRoll] = 0,
                    [StatTypes.Armor] = 0
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
            new Position { Value = Positions.Standing },
            new Location { Room = room },
            new DirtyStats() // dirty by default
        );

        room.Get<RoomContents>().Characters.Add(player);

        return player;
    }
}
