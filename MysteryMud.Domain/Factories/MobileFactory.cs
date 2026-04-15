using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Factories;

public static class MobileFactory
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
            new Health { Current = 5, Max = 100 },
            new BaseHealth {  Max = 100 },
            new HealthRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 },
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
            new ThreatTable { Threat = [] },
            new DirtyStats() // dirty by default
        );

        room.Get<RoomContents>().Characters.Add(player);

        return player;
    }
}
