using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Helpers;

public static class CharacterHelpers
{
    public static bool IsAlive(World world, params EntityId[] entities)
    {
        return entities.All(x => world.IsAlive(x) && !world.Has<Dead>(x));
    }

    public static bool SameRoom(World world, EntityId character1, EntityId character2)
        => world.Get<Location>(character1).Room == world.Get<Location>(character2).Room;

    public static PositionKind Position(World world, EntityId entity)
       => world.TryGet<Position>(entity, out var position) ? position.Value : PositionKind.Dead;

    public static int Level(World world, EntityId entity)
        => world.TryGet<Level>(entity, out var level) ? level.Value : 1;

    public static bool HasAutoAssist(World world, EntityId entity)
        => HasAuto(world, entity, AutoFlags.Assist);

    public static bool HasAutoLoot(World world, EntityId entity)
        => HasAuto(world, entity, AutoFlags.Loot);

    public static bool HasAutoSacrifice(World world, EntityId entity)
        => HasAuto(world, entity, AutoFlags.Sacrifice);

    public static bool HasAuto(World world, EntityId entity, AutoFlags flag)
    {
        ref var autoBehavior = ref world.TryGetRef<AutoBehaviour>(entity, out var hasAutoBehavior);
        if (!hasAutoBehavior)
            return false;
        return autoBehavior.Flags.HasFlag(flag);
    }

    public static bool TryGetMainHandWeapon(World world, EntityId entity, out EntityId item, out Weapon weapon)
    {
        weapon = default;
        item = default;
        ref var equipment = ref world.TryGetRef<Equipment>(entity, out var hasEquipment);
        if (!hasEquipment)
            return false;
        if (!equipment.Slots.TryGetValue(EquipmentSlotKind.MainHand, out item) || item == default)
            return false;
        if (!world.TryGet(item, out weapon))
            return false;
        return true;
    }
}
