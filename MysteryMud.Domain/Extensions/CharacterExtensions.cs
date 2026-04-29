using DefaultEcs;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Extensions;

public static class CharacterExtensions
{
    extension(Entity entity)
    {
        public PositionKind Position => entity.Has<Position>() ? entity.Get<Position>().Value : PositionKind.Dead;
        public int Level => entity.Has<Level>() ? entity.Get<Level>().Value : 1;
    }

    public static bool HasAutoAssist(this Entity entity)
        => HasAuto(entity, AutoFlags.Assist);

    public static bool HasAutoLoot(this Entity entity)
        => HasAuto(entity, AutoFlags.Loot);

    public static bool HasAutoSacrifice(this Entity entity)
        => HasAuto(entity, AutoFlags.Sacrifice);

    public static bool HasAuto(this Entity entity, AutoFlags flag)
    {
        if (!entity.Has<AutoBehaviour>())
            return false;
        ref var autoBehavior = ref entity.Get<AutoBehaviour>();
        return autoBehavior.Flags.HasFlag(flag);
    }

    public static bool TryGetMainHandWeapon(this Entity entity, out Entity item, out Weapon weapon)
    {
        weapon = default;
        item = default;
        if (!entity.Has<Equipment>())
            return false;
        ref var equipment = ref entity.Get<Equipment>();
        if (!equipment.Slots.TryGetValue(EquipmentSlotKind.MainHand, out item) || item == default)
            return false;
        if (!item.Has<Weapon>())
            return false;
        weapon = item.Get<Weapon>();
        return true;
    }
}
