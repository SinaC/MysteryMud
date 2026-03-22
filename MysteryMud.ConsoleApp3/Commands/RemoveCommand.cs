using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class RemoveCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.Target;

    public void Execute(World world, Entity actor, CommandContext ctx)
    {
        ref var equipment = ref actor.Get<Equipment>();

        // TODO: remove all ? remove x.something
        foreach (var kv in equipment.Slots)
        {
            var item = kv.Value;

            if (TargetingSystem.Matches(item, ctx.Primary.Name))
            {
                EquipmentSystem.Unequip(actor, kv.Key);

                MessageSystem.Send(actor, $"You remove {item.DisplayName}.");
                return;
            }
        }

        MessageSystem.Send(actor, "You are not wearing that.");
    }
}
