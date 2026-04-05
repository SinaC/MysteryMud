using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components.Characters;

namespace MysteryMud.Application.Commands;

public class DestroyCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    public void Execute(CommandExecutionContext executionContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            executionContext.Msg.To(actor).Send("Destroy what ?");
            return;
        }

        // search in inventory (equipped items are also in inventory)
        ref var inventory = ref actor.Get<Inventory>();

        foreach (var item in EntityFinder.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            // intent to destroy item
            ref var destroyItemIntent = ref executionContext.Intent.DestroyItem.Add();
            destroyItemIntent.Entity = actor;
            destroyItemIntent.Item = item;
        }
    }
}
