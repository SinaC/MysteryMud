using Arch.Core;
using MysteryMud.ConsoleApp.Commands;
using MysteryMud.ConsoleApp.Components;

namespace MysteryMud.ConsoleApp.Systems;

static class CommandSystem
{
    public static void Run(World world, CommandQueue queue)
    {
        while (queue.Commands.Count > 0)
        {
            var cmd = queue.Commands.Dequeue();

            if (cmd is AttackCommand atk)
            {
                world.Add(atk.Attacker, new Target { Value = atk.Target });
            }
            if (cmd is LookCommand look)
            {
                LookSystem.Execute(world, look.Actor);
            }
            if (cmd is EquipCommand equip)
            {
                EquipSystem.Run(world, equip.Actor, equip.ItemName);
            }
        }
    }
}
