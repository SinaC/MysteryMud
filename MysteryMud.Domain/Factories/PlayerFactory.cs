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
            new CommandBuffer(),
            new CommandThrottle(),
            new Name { Value = name },
            new BaseStats
            {
                Level = 1,
                Experience = 0,
                Values = new Dictionary<StatKind, int>
                {
                    [StatKind.Strength] = 15,
                    [StatKind.Intelligence] = 10,
                    [StatKind.Wisdom] = 15,
                    [StatKind.Dexterity] = 12,
                    [StatKind.Constitution] = 15,
                    [StatKind.HitRoll] = 0,
                    [StatKind.DamRoll] = 0,
                    [StatKind.Armor] = 0
                }
            },
            new EffectiveStats
            {
                Level = 1,
                Experience = 0,
                Values = new Dictionary<StatKind, int>
                {
                    [StatKind.Strength] = 15,
                    [StatKind.Intelligence] = 10,
                    [StatKind.Wisdom] = 15,
                    [StatKind.Dexterity] = 12,
                    [StatKind.Constitution] = 15,
                    [StatKind.HitRoll] = 0,
                    [StatKind.DamRoll] = 0,
                    [StatKind.Armor] = 0
                }
            },
            new Health { Current = 10, Max = 100 },
            new Inventory { Items = [] },
            new Equipment { Slots = [] },
            new CharacterEffects
            {
                Effects = [],
                EffectsByTag = new List<Entity>?[32]
            },
            new Position { Value = PositionKind.Standing },
            new Location { Room = room },
            new DirtyStats() // dirty by default
        );

        room.Get<RoomContents>().Characters.Add(player);

        return player;
    }
}
