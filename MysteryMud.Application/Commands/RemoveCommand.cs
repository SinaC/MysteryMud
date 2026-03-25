using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class RemoveCommand : ICommand
{
    public CommandParseOptions ParseOptions => ICommand.Target; 
    public CommandDefinition Definition { get; }

    public RemoveCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount == 0)
        {
            systemContext.MessageBus.Publish(actor, "Remove what ?");
            return;
        }

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
