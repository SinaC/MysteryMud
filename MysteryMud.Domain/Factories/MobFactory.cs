using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Data.Enums;

namespace MysteryMud.Domain.Factories;

public static class MobFactory
{
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
}
