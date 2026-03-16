using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Systems;

// important note: even when worn item stays in inventory
public static class EquipmentSystem
{
    // TODO: auto replace
    public static bool Equip(Entity actor, Entity item)
    {
        ref var equipable = ref item.Get<Equipable>();
        ref var equipment = ref actor.Get<Equipment>();

        var slot = equipable.Slot;

        if (equipment.Slots.ContainsKey(slot))
            return false;

        equipment.Slots[slot] = item;

        item.Add(new Equipped
        {
            Wearer = actor,
            Slot = slot
        });

        actor.Add<DirtyStats>();

        return true;
    }

    public static void Unequip(Entity actor, EquipmentSlot slot)
    {
        ref var equipment = ref actor.Get<Equipment>();

        if (!equipment.Slots.TryGetValue(slot, out var item))
            return;

        equipment.Slots.Remove(slot);

        item.Remove<Equipped>();

        actor.Add<DirtyStats>();
    }
}
