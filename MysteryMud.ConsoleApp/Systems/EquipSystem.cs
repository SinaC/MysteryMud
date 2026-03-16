using Arch.Core;
using MysteryMud.ConsoleApp.Components;

namespace MysteryMud.ConsoleApp.Systems;

static class EquipSystem
{
    public static void Run(World world, Entity actor, string itemName)
    {
        ref var inv = ref world.Get<Inventory>(actor);

        Entity item = default;
        bool found = false;

        foreach (var i in inv.Items)
        {
            var itemData = world.Get<Item>(i);

            if (itemData.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
            {
                item = i;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Console.WriteLine("You don't have that item.");
            return;
        }

        EquipItem(world, actor, item);
        inv.Items.Remove(item);
    }

    static void EquipItem(World world, Entity actor, Entity item)
    {
        ref var eq = ref world.Get<Equipment>(actor);

        eq.Slots[EquipSlot.Weapon] = item;

        world.Get<StatsDirty>(actor).Value = true;

        if (world.Has<LightSource>(item))
        {
            world.Add(actor, world.Get<LightSource>(item));

            Console.WriteLine("You light the torch.");
        }
    }
}
