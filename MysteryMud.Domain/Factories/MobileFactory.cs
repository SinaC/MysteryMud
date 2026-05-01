using DefaultEcs;
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
        var mob = world.CreateEntity();
        mob.Set(new CharacterTag());
        mob.Set(new NpcTag());
        mob.Set(new CommandLevel { Value = CommandLevelKind.Player });
        mob.Set(new CommandBuffer());
        mob.Set(new Name { Value = name });
        mob.Set(new Level { Value = 1 });
        mob.Set(new Description { Value = description });
        mob.Set(new BaseStats
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
        mob.Set(new EffectiveStats
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
        mob.Set(new Health { Current = 5, Max = 100 });
        mob.Set(new BaseHealth { Max = 100 });
        mob.Set(new HealthRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        mob.Set(new Move { Current = 100, Max = 100 });
        mob.Set(new BaseMove { Max = 100 });
        mob.Set(new MoveRegen { BaseAmountPerSecond = 1, CurrentAmountPerSecond = 1 });
        mob.Set(new Inventory { Items = [] });
        mob.Set(new Equipment { Slots = [] });
        mob.Set(new CharacterEffects
        {
            Data = new EffectsCollection
            {
                Effects = [],
                EffectsByTag = new List<Entity>?[32]
            },
        });
        mob.Set(new BaseIRV { Immunities = 0, Resistances = 0, Vulnerabilities = 0 });
        mob.Set(new EffectiveIRV { Immunities = 0, Resistances = 0, Vulnerabilities = 0 });
        mob.Set(new Location { Room = room });
        mob.Set(new Position { Value = PositionKind.Standing });
        mob.Set(new ThreatTable { Entries = [] });
        mob.Set(new DirtyStats()); // dirty by default
        mob.Set(new DirtyIRV()); // dirty by default

        room.Get<RoomContents>().Characters.Add(mob);

        return mob;
    }
}
