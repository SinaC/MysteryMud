using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class PutCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.TargetPair;
    public CommandDefinition Definition { get; }

    public PutCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount < 2)
        {
            systemContext.Msg.To(actor).Send("Put what in what ?");
            return;
        }

        var inventory = actor.Get<Inventory>();

        var container = EntityFinder.FindContainer(actor, ctx.Secondary);
        if (container == default)
        {
            systemContext.Msg.To(actor).Send("You don't see that here.");
            return;
        }

        foreach (var item in EntityFinder.SelectTargets(actor, ctx.Primary, inventory.Items).Where(x => x != container))
        {
            // intent to put item in container
            ref var putItemIntent = ref systemContext.Intent.PutItem.Add();
            putItemIntent.Entity = actor;
            putItemIntent.Item = item;
            putItemIntent.Container = container;
        }
    }
}
