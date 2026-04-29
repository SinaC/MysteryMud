using DefaultEcs;
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
        var player = world.CreateEntity();
        player.Set(new Connection
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
        var player = world.CreateEntity();
        player.Set(new CharacterTag());
        player.Set(new PlayerTag());
        player.Set(new CommandLevel { Value = CommandLevelKind.Player });
        player.Set(new CommandBuffer());
        player.Set(commandThrottle);
        player.Set(new Name { Value = name });
        player.Set(new Level { Value = 1 });
        player.Set(new Progression { Experience = 0, ExperienceByLevel = 1000, ExperienceToNextLevel = 2000 });
        player.Set(new BaseStats
        {
            Values = CharacterStatValues.From(
                (CharacterStatKind.Strength, 15),
                (CharacterStatKind.Intelligence, 10),
                (CharacterStatKind.Wisdom, 15),
                (CharacterStatKind.Dexterity, 12),
                (CharacterStatKind.Constitution, 15),
                (CharacterStatKind.SavingThrow, 0),
                (CharacterStatKind.HitRoll, 0),
                (CharacterStatKind.DamRoll, 0))
        });
        player.Set(new EffectiveStats
        {
            Values = CharacterStatValues.From(
                (CharacterStatKind.Strength, 15),
                (CharacterStatKind.Intelligence, 10),
                (CharacterStatKind.Wisdom, 15),
                (CharacterStatKind.Dexterity, 12),
                (CharacterStatKind.Constitution, 15),
                (CharacterStatKind.SavingThrow, 0),
                (CharacterStatKind.HitRoll, 0),
                (CharacterStatKind.DamRoll, 0))
        });
        player.Set(new Form { Value = FormType.Humanoid });
        player.Set(new Inventory { Items = [] });
        player.Set(new Equipment { Slots = [] });
        player.Set(new CharacterEffects
            {
                Data = new EffectsCollection
                {
                    Effects = [],
                    EffectsByTag = new List<Entity>?[32]
                },
            });
        player.Set(new IRV { Immunities = 0, Resistances = 0, Vulnerabilities = 0 });
        player.Set(new Location { Room = room });
        player.Set(new Position { Value = PositionKind.Standing });
        player.Set(new AutoBehaviour { Flags = AutoFlags.Loot | AutoFlags.Sacrifice | AutoFlags.Assist });
        player.Set(new DirtyStats()); // dirty by default
        player.Set(new Health { Current = 10, Max = 100 });
        player.Set(new BaseHealth { Max = 100 });
        player.Set(new HealthRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Set(new Move { Current = 100, Max = 100 });
        player.Set(new BaseMove { Max = 100 });
        player.Set(new MoveRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Set(new Mana { Current = 100, Max = 100 });
        player.Set(new BaseMana { Max = 100 });
        player.Set(new ManaRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Set(new UsesMana());
        player.Set(new Energy { Current = 100, Max = 100 });
        player.Set(new BaseEnergy { Max = 100 });
        player.Set(new EnergyRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Set(new Rage { Current = 0, Max = 100 });
        player.Set(new BaseRage { Max = 100 });
        player.Set(new RageDecay { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });

        room.Get<RoomContents>().Characters.Add(player);

        return player;
    }

    public static Entity CreateAdmin(World world, string name, Entity room)
    {
        var commandThrottle = new CommandThrottle();
        CommandThrottlingFactory.Initialize(ref commandThrottle);
        var player = world.CreateEntity();
        player.Set(new CharacterTag());
        player.Set(new PlayerTag());
        player.Set(new CommandLevel { Value = CommandLevelKind.Admin });
        player.Set(new CommandBuffer());
        player.Set(commandThrottle);
        player.Set(new Name { Value = name });
        player.Set(new Level { Value = 100 });
        player.Set(new Progression { Experience = 1000000, ExperienceByLevel = 1000, ExperienceToNextLevel = 0 });
        player.Set(new BaseStats
        {
            Values = CharacterStatValues.From(
                (CharacterStatKind.Strength, 15),
                (CharacterStatKind.Intelligence, 10),
                (CharacterStatKind.Wisdom, 15),
                (CharacterStatKind.Dexterity, 12),
                (CharacterStatKind.Constitution, 15),
                (CharacterStatKind.SavingThrow, 0),
                (CharacterStatKind.HitRoll, 0),
                (CharacterStatKind.DamRoll, 0))
        });
        player.Set(new EffectiveStats
        {
            Values = CharacterStatValues.From(
                (CharacterStatKind.Strength, 15),
                (CharacterStatKind.Intelligence, 10),
                (CharacterStatKind.Wisdom, 15),
                (CharacterStatKind.Dexterity, 12),
                (CharacterStatKind.Constitution, 15),
                (CharacterStatKind.SavingThrow, 0),
                (CharacterStatKind.HitRoll, 0),
                (CharacterStatKind.DamRoll, 0))
        });
        player.Set(new Form { Value = FormType.Humanoid });
        player.Set(new Inventory { Items = [] });
        player.Set(new Equipment { Slots = [] });
        player.Set(new CharacterEffects
        {
            Data = new EffectsCollection
            {
                Effects = [],
                EffectsByTag = new List<Entity>?[32]
            },
        });
        player.Set(new IRV { Immunities = 0, Resistances = 0, Vulnerabilities = 0 });
        player.Set(new Location { Room = room });
        player.Set(new Position { Value = PositionKind.Standing });
        player.Set(new AutoBehaviour { Flags = AutoFlags.Loot | AutoFlags.Sacrifice | AutoFlags.Assist });
        player.Set(new DirtyStats()); // dirty by default
        player.Set(new Health { Current = 10000, Max = 10000 });
        player.Set(new BaseHealth { Max = 10000 });
        player.Set(new HealthRegen { BaseAmountPerSecond = 100, CurrentAmountPerSecond = 100 });
        player.Set(new Move { Current = 1000, Max = 1000 });
        player.Set(new BaseMove { Max = 1000 });
        player.Set(new MoveRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Set(new Mana { Current = 100, Max = 100 });
        player.Set(new BaseMana { Max = 100 });
        player.Set(new ManaRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Set(new UsesMana());
        player.Set(new Energy { Current = 100, Max = 100 });
        player.Set(new BaseEnergy { Max = 100 });
        player.Set(new EnergyRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Set(new Rage { Current = 0, Max = 100 });
        player.Set(new BaseRage { Max = 100 });
        player.Set(new RageDecay { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });

        room.Get<RoomContents>().Characters.Add(player);

        return player;
    }

    public static void InitializePlayer(Entity player)
    {
        // TODO: read from pfile
        var commandThrottle = new CommandThrottle();
        CommandThrottlingFactory.Initialize(ref commandThrottle);
        player.Set(new CharacterTag());
        player.Set(new PlayerTag());
        player.Set(new CommandLevel { Value = CommandLevelKind.Admin });
        player.Set(new CommandBuffer());
        player.Set(commandThrottle);
        player.Set(new Name { Value = "joel" }); // TODO: implement character creation and loading from file, for now just use a placeholder name
        player.Set(new Level { Value = 50 });
        player.Set(new Progression { Experience = 49950, ExperienceByLevel = 1000, ExperienceToNextLevel = 50000 });
        player.Set(new BaseStats
        {
            Values = CharacterStatValues.From(
                (CharacterStatKind.Strength, 15),
                (CharacterStatKind.Intelligence, 10),
                (CharacterStatKind.Wisdom, 15),
                (CharacterStatKind.Dexterity, 12),
                (CharacterStatKind.Constitution, 15),
                (CharacterStatKind.SavingThrow, 0),
                (CharacterStatKind.HitRoll, 0),
                (CharacterStatKind.DamRoll, 0))
        });
        player.Set(new EffectiveStats
        {
            Values = CharacterStatValues.From(
                (CharacterStatKind.Strength, 15),
                (CharacterStatKind.Intelligence, 10),
                (CharacterStatKind.Wisdom, 15),
                (CharacterStatKind.Dexterity, 12),
                (CharacterStatKind.Constitution, 15),
                (CharacterStatKind.SavingThrow, 0),
                (CharacterStatKind.HitRoll, 0),
                (CharacterStatKind.DamRoll, 0))
        });
        player.Set(new Form { Value = FormType.Humanoid });
        player.Set(new Inventory { Items = [] });
        player.Set(new Equipment { Slots = [] });
        player.Set(new CharacterEffects
        {
            Data = new EffectsCollection
            {
                Effects = [],
                EffectsByTag = new List<Entity>?[32]
            },
        });
        player.Set(new IRV { Immunities = 0, Resistances = 0, Vulnerabilities = 0 });
        player.Set(new Location { Room = RoomFactory.StartingRoomEntity });
        player.Set(new Position { Value = PositionKind.Standing });
        player.Set(new AutoBehaviour { Flags = AutoFlags.Loot | AutoFlags.Sacrifice | AutoFlags.Assist });
        player.Set(new DirtyStats()); // ensure stats are recomputed

        player.Set(new Health { Current = 10000, Max = 10000 });
        player.Set(new BaseHealth { Max = 10000 });
        player.Set(new HealthRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Set(new Move { Current = 35, Max = 1000 });
        player.Set(new BaseMove { Max = 1000 });
        player.Set(new MoveRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Set(new Mana { Current = 100, Max = 1000 });
        player.Set(new BaseMana { Max = 100 });
        player.Set(new ManaRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        player.Set(new UsesMana());
        player.Set(new Energy { Current = 100, Max = 100 });
        player.Set(new BaseEnergy { Max = 100 });
        player.Set(new EnergyRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 11 });
        player.Set(new Rage { Current = 0, Max = 100 });
        player.Set(new BaseRage { Max = 100 });
        player.Set(new RageDecay { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });

        RoomFactory.StartingRoomEntity.Get<RoomContents>().Characters.Add(player); // move to starting room
    }
}
