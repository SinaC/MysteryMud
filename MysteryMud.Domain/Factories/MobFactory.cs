using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Factories;

public static class MobFactory
{
    public static Entity CreateMob(World world, string name, string description, Entity room)
    {
        var player = world.Create(
            new CharacterTag(),
            new NpcTag(),
            new CommandLevel { Value = CommandLevelKind.Player },
            new CommandBuffer(),
            new Name { Value = name },
            new Level { Value = 1 },
            new Description { Value = description },
            new BaseStats
            {
                Values = StatValues.From(
                    (StatKind.Strength, 15),
                    (StatKind.Intelligence, 10),
                    (StatKind.Wisdom, 15),
                    (StatKind.Dexterity, 12),
                    (StatKind.Constitution, 15),
                    (StatKind.HitRoll, 0),
                    (StatKind.DamRoll, 0),
                    (StatKind.ArmorClass, 0))
            },
            new EffectiveStats
            {
                Values = StatValues.From(
                    (StatKind.Strength, 15),
                    (StatKind.Intelligence, 10),
                    (StatKind.Wisdom, 15),
                    (StatKind.Dexterity, 12),
                    (StatKind.Constitution, 15),
                    (StatKind.HitRoll, 0),
                    (StatKind.DamRoll, 0),
                    (StatKind.ArmorClass, 0))
            },
            new Health { Current = 5, Max = 100 },
            new BaseHealth {  Max = 100 },
            new Inventory { Items = [] },
            new Equipment { Slots = [] },
            new CharacterEffects
            {
                Effects = [],
                EffectsByTag = new List<Entity>?[32]
            },
            new Location { Room = room },
            new Position { Value = PositionKind.Standing },
            new ThreatTable { Threat = [] },
            new DirtyStats() // dirty by default
        );

        room.Get<RoomContents>().Characters.Add(player);

        return player;
    }
}
