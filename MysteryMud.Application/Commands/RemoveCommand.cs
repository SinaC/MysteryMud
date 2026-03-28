using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.OldSystems;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class RemoveCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.Target; 
    public CommandDefinition Definition { get; }

    public RemoveCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.To(actor).Send("Remove what ?");
            return;
        }

        ref var equipment = ref actor.Get<Equipment>();

        // TODO: remove all ? remove x.something
        foreach (var kv in equipment.Slots)
        {
            var item = kv.Value;

            if (EntityFinder.Matches(item, ctx.Primary.Name))
            {
                EquipmentSystem.Unequip(actor, kv.Key);

                systemContext.Msg.To(actor).Send($"You remove {item.DisplayName}.");
                return;
            }
        }

        systemContext.Msg.To(actor).Send("You are not wearing that.");
    }
}
