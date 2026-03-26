using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Mobiles;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Factories;

public static class MobFactory
{
    public static Entity CreateMob(World world, string name, string description, Entity room)
    {
        var player = world.Create(
            new CharacterTag(),
            new NpcTag(),
            new Name { Value = name },
            new Description { Value = description },
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
            new Health { Current = 5, Max = 100 },
            new Inventory { Items = [] },
            new Equipment { Slots = [] },
            new CharacterEffects
            {
                Effects = [],
                EffectsByTag = new Entity?[32]
            },
            new Location { Room = room },
            new Position { Value = Positions.Standing },
            new ThreatTable { Threat = [] },
            new DirtyStats() // dirty by default
        );

        room.Get<RoomContents>().Characters.Add(player);

        return player;
    }
}
