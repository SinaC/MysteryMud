using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Helpers;

public static class CharacterHelpers
{
    public static bool IsAlive(params Entity[] entities)
    {
        return entities.All(x => x.IsAlive() && !x.Has<Dead>());
    }

    public static bool TryGetMainHandWeapon(Entity entity, out Entity item, out Weapon weapon)
    {
        weapon = default;
        item = default;
        ref var equipment = ref entity.TryGetRef<Equipment>(out var hasEquipment);
        if (!hasEquipment)
            return false;
        if (!equipment.Slots.TryGetValue(EquipmentSlotKind.MainHand, out item) || item == default)
            return false;
        if (!item.TryGet(out weapon))
            return false;
        return true;
    }
}
