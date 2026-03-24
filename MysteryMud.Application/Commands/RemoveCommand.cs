using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Systems;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain;
using MysteryMud.Domain.Components.Characters;

namespace MysteryMud.Application.Commands;

public class RemoveCommand : ICommand
{
    public CommandParseOptions ParseOptions => ICommand.Target;

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        ref var equipment = ref actor.Get<Equipment>();

        // TODO: remove all ? remove x.something
        foreach (var kv in equipment.Slots)
        {
            var item = kv.Value;

            if (TargetingSystem.Matches(item, ctx.Primary.Name))
            {
                EquipmentSystem.Unequip(actor, kv.Key);

                systemContext.MessageBus.Publish(actor, $"You remove {item.DisplayName}.");
                return;
            }
        }

        systemContext.MessageBus.Publish(actor, "You are not wearing that.");
    }
}
