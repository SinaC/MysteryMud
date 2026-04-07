using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;
using System.Numerics;

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
        var commandThrottle = new CommandThrottle();
        CommandThrottlingFactory.Initialize(ref commandThrottle);
        var player = world.Create(
            new CharacterTag(),
            new PlayerTag(),
            new CommandLevel { Value = CommandLevelKind.Player },
            new CommandBuffer(),
            commandThrottle,
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
                    [StatKind.ArmorClass] = 0
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
                    [StatKind.ArmorClass] = 0
                }
            },
            new Form { Value = FormType.Humanoid },
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
        player.Add(
            new Health { Current = 10, Max = 100 },
            new BaseHealth { Max = 100 },
            new HealthRegen { AmountPerTick = 1 });
        player.Add(
            new Mana { Current = 100, Max = 100 },
            new BaseMana { Max = 100 },
            new ManaRegen { AmountPerTick = 1 },
            new UsesMana());
        player.Add(
            new Energy { Current = 100, Max = 100 },
            new BaseEnergy { Max = 100 },
            new EnergyRegen { AmountPerTick = 1 });
        player.Add(
            new Rage { Current = 0, Max = 100 },
            new BaseRage { Max = 100 },
            new RageDecay { AmountPerTick = 1 });

        room.Get<RoomContents>().Characters.Add(player);

        return player;
    }

    public static Entity CreateAdmin(World world, string name, Entity room)
    {
        var commandThrottle = new CommandThrottle();
        CommandThrottlingFactory.Initialize(ref commandThrottle);
        var player = world.Create(
            new CharacterTag(),
            new PlayerTag(),
            new CommandLevel { Value = CommandLevelKind.Admin },
            new CommandBuffer(),
            commandThrottle,
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
                    [StatKind.ArmorClass] = 0
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
                    [StatKind.ArmorClass] = 0
                }
            },
            new Form { Value = FormType.Humanoid },
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
        player.Add(
            new Health { Current = 10, Max = 100 },
            new BaseHealth { Max = 100 },
            new HealthRegen { AmountPerTick = 1 });
        player.Add(
            new Mana { Current = 100, Max = 100 },
            new BaseMana { Max = 100 },
            new ManaRegen { AmountPerTick = 1 },
            new UsesMana());
        player.Add(
            new Energy { Current = 100, Max = 100 },
            new BaseEnergy { Max = 100 },
            new EnergyRegen { AmountPerTick = 1 });
        player.Add(
            new Rage { Current = 0, Max = 100 },
            new BaseRage { Max = 100 },
            new RageDecay { AmountPerTick = 1 });

        room.Get<RoomContents>().Characters.Add(player);

        return player;
    }

    public static void InitializePlayer(Entity player)
    {
        var commandThrottle = new CommandThrottle();
        CommandThrottlingFactory.Initialize(ref commandThrottle);
        player.Add(
            new CharacterTag(),
            new PlayerTag(),
            new CommandLevel { Value = CommandLevelKind.Admin },
            new CommandBuffer(),
            commandThrottle,
            new Name { Value = "joel" }, // TODO: implement character creation and loading from file, for now just use a placeholder name
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
                    [StatKind.ArmorClass] = 0
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
                    [StatKind.ArmorClass] = 0
                }
            },
            new Form { Value = FormType.Humanoid },
            new Inventory { Items = [] },
            new Equipment { Slots = [] },
            new CharacterEffects
            {
                Effects = [],
                EffectsByTag = new List<Entity>?[32]
            },
            new Location { Room = RoomFactory.StartingRoomEntity },
            new Position { Value = PositionKind.Standing },
            new DirtyStats()); // ensure stats are recomputed

        player.Add(
            new Health { Current = 10, Max = 100 },
            new BaseHealth { Max = 100 },
            new HealthRegen { AmountPerTick = 1 });
        player.Add(
            new Mana { Current = 100, Max = 100 },
            new BaseMana { Max = 100 },
            new ManaRegen { AmountPerTick = 1 },
            new UsesMana());
        player.Add(
            new Energy { Current = 100, Max = 100 },
            new BaseEnergy { Max = 100 },
            new EnergyRegen { AmountPerTick = 1 });
        player.Add(
            new Rage { Current = 0, Max = 100 },
            new BaseRage { Max = 100 },
            new RageDecay { AmountPerTick = 1 });

        RoomFactory.StartingRoomEntity.Get<RoomContents>().Characters.Add(player); // move to starting room
    }
}
