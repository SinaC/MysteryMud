using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Characters.Resources;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Definitions;
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
        var commandThrottle = new CommandThrottle();
        CommandThrottlingFactory.Initialize(ref commandThrottle);
        var player = world.Create(
            new CharacterTag(),
            new PlayerTag(),
            new CommandLevel { Value = CommandLevelKind.Player },
            new CommandBuffer(),
            commandThrottle,
            new Name { Value = name },
            new Level { Value = 1 },
            new Progression { Experience = 0, ExperienceByLevel = 1000, ExperienceToNextLevel = 2000 },
            new BaseStats
            {
                Values = CharacterStatValues.From(
                    (CharacterStatKind.Strength, 15),
                    (CharacterStatKind.Intelligence, 10),
                    (CharacterStatKind.Wisdom, 15),
                    (CharacterStatKind.Dexterity, 12),
                    (CharacterStatKind.Constitution, 15),
                    (CharacterStatKind.HitRoll, 0),
                    (CharacterStatKind.DamRoll, 0),
                    (CharacterStatKind.ArmorClass, 0))
            },
            new EffectiveStats
            {
                Values = CharacterStatValues.From(
                    (CharacterStatKind.Strength, 15),
                    (CharacterStatKind.Intelligence, 10),
                    (CharacterStatKind.Wisdom, 15),
                    (CharacterStatKind.Dexterity, 12),
                    (CharacterStatKind.Constitution, 15),
                    (CharacterStatKind.HitRoll, 0),
                    (CharacterStatKind.DamRoll, 0),
                    (CharacterStatKind.ArmorClass, 0))
            },
            new Form { Value = FormType.Humanoid },
            new Inventory { Items = [] },
            new Equipment { Slots = [] },
            new CharacterEffects
            {
                Data = new EffectsCollection
                {
                    Effects = [],
                    EffectsByTag = new List<Entity>?[32]
                },
            },
            new Location { Room = room },
            new Position { Value = PositionKind.Standing },
            new AutoBehaviour { Flags = AutoFlags.Loot | AutoFlags.Sacrifice | AutoFlags.Assist },
            new DirtyStats() // dirty by default
        );
        player.Add(
            new Health { Current = 10, Max = 100 },
            new BaseHealth { Max = 100 },
            new HealthRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Add(
            new Move { Current = 100, Max = 100 },
            new BaseMove { Max = 100 },
            new MoveRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Add(
            new Mana { Current = 100, Max = 100 },
            new BaseMana { Max = 100 },
            new ManaRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 },
            new UsesMana());
        player.Add(
            new Energy { Current = 100, Max = 100 },
            new BaseEnergy { Max = 100 },
            new EnergyRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Add(
            new Rage { Current = 0, Max = 100 },
            new BaseRage { Max = 100 },
            new RageDecay { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });

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
            new Level { Value = 100 },
            new Progression { Experience = 1000000, ExperienceByLevel = 1000, ExperienceToNextLevel = 0 },
            new BaseStats
            {
                Values = CharacterStatValues.From(
                    (CharacterStatKind.Strength, 15),
                    (CharacterStatKind.Intelligence, 10),
                    (CharacterStatKind.Wisdom, 15),
                    (CharacterStatKind.Dexterity, 12),
                    (CharacterStatKind.Constitution, 15),
                    (CharacterStatKind.HitRoll, 0),
                    (CharacterStatKind.DamRoll, 0),
                    (CharacterStatKind.ArmorClass, 0))
            },
            new EffectiveStats
            {
                Values = CharacterStatValues.From(
                    (CharacterStatKind.Strength, 15),
                    (CharacterStatKind.Intelligence, 10),
                    (CharacterStatKind.Wisdom, 15),
                    (CharacterStatKind.Dexterity, 12),
                    (CharacterStatKind.Constitution, 15),
                    (CharacterStatKind.HitRoll, 0),
                    (CharacterStatKind.DamRoll, 0),
                    (CharacterStatKind.ArmorClass, 0))
            },
            new Form { Value = FormType.Humanoid },
            new Inventory { Items = [] },
            new Equipment { Slots = [] },
            new CharacterEffects
            {
                Data = new EffectsCollection
                {
                    Effects = [],
                    EffectsByTag = new List<Entity>?[32]
                },
            },
            new Location { Room = room },
            new Position { Value = PositionKind.Standing },
            new AutoBehaviour { Flags = AutoFlags.Loot | AutoFlags.Sacrifice | AutoFlags.Assist },
            new DirtyStats() // dirty by default
        );
        player.Add(
            new Health { Current = 10000, Max = 10000 },
            new BaseHealth { Max = 10000 },
            new HealthRegen { BaseAmountPerSecond = 100, CurrentAmountPerSecond = 100 });
        player.Add(
            new Move { Current = 1000, Max = 1000 },
            new BaseMove { Max = 1000 },
            new MoveRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Add(
            new Mana { Current = 100, Max = 100 },
            new BaseMana { Max = 100 },
            new ManaRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 },
            new UsesMana());
        player.Add(
            new Energy { Current = 100, Max = 100 },
            new BaseEnergy { Max = 100 },
            new EnergyRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Add(
            new Rage { Current = 0, Max = 100 },
            new BaseRage { Max = 100 },
            new RageDecay { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });

        room.Get<RoomContents>().Characters.Add(player);

        return player;
    }

    public static void InitializePlayer(Entity player)
    {
        // TODO: read from pfile
        var commandThrottle = new CommandThrottle();
        CommandThrottlingFactory.Initialize(ref commandThrottle);
        player.Add(
            new CharacterTag(),
            new PlayerTag(),
            new CommandLevel { Value = CommandLevelKind.Admin },
            new CommandBuffer(),
            commandThrottle,
            new Name { Value = "joel" }, // TODO: implement character creation and loading from file, for now just use a placeholder name
            new Level { Value = 50 },
            new Progression { Experience = 49950, ExperienceByLevel = 1000, ExperienceToNextLevel = 50000 },
            new BaseStats
            {
                Values = CharacterStatValues.From(
                    (CharacterStatKind.Strength, 15),
                    (CharacterStatKind.Intelligence, 10),
                    (CharacterStatKind.Wisdom, 15),
                    (CharacterStatKind.Dexterity, 12),
                    (CharacterStatKind.Constitution, 15),
                    (CharacterStatKind.HitRoll, 0),
                    (CharacterStatKind.DamRoll, 0),
                    (CharacterStatKind.ArmorClass, 0))
            },
            new EffectiveStats
            {
                Values = CharacterStatValues.From(
                    (CharacterStatKind.Strength, 15),
                    (CharacterStatKind.Intelligence, 10),
                    (CharacterStatKind.Wisdom, 15),
                    (CharacterStatKind.Dexterity, 12),
                    (CharacterStatKind.Constitution, 15),
                    (CharacterStatKind.HitRoll, 0),
                    (CharacterStatKind.DamRoll, 0),
                    (CharacterStatKind.ArmorClass, 0))
            },
            new Form { Value = FormType.Humanoid },
            new Inventory { Items = [] },
            new Equipment { Slots = [] },
            new CharacterEffects
            {
                Data = new EffectsCollection
                {
                    Effects = [],
                    EffectsByTag = new List<Entity>?[32]
                },
            },
            new Location { Room = RoomFactory.StartingRoomEntity },
            new Position { Value = PositionKind.Standing },
            new AutoBehaviour { Flags = AutoFlags.Loot | AutoFlags.Sacrifice | AutoFlags.Assist },
            new DirtyStats() // ensure stats are recomputed
            );

        player.Add(
            new Health { Current = 10000, Max = 10000 },
            new BaseHealth { Max = 10000 },
            new HealthRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Add(
            new Move { Current = 35, Max = 1000 },
            new BaseMove { Max = 1000 },
            new MoveRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Add(
            new Mana { Current = 100, Max = 1000 },
            new BaseMana { Max = 100 },
            new ManaRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 },
            new UsesMana());
        player.Add(
            new Energy { Current = 100, Max = 100 },
            new BaseEnergy { Max = 100 },
            new EnergyRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 11 });
        player.Add(
            new Rage { Current = 0, Max = 100 },
            new BaseRage { Max = 100 },
            new RageDecay { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });

        RoomFactory.StartingRoomEntity.Get<RoomContents>().Characters.Add(player); // move to starting room
    }
}
