using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class RemoveCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target; 

    public CommandDefinition Definition { get; }

    public RemoveCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.To(actor).Send("Remove what ?");
            return;
        }

        ref var equipment = ref actor.Get<Equipment>();

        foreach (var kv in equipment.Slots)
        {
            var slot = kv.Key;
            var item = kv.Value;

            if (EntityFinder.Matches(item, ctx.Primary.Name))
            {
                // intent to remove item
                ref var removeItemIntent = ref systemContext.Intent.RemoveItem.Add();
                removeItemIntent.Actor = actor;
                removeItemIntent.Item = item;
                removeItemIntent.Slot = slot;
            }
        }
    }
}
