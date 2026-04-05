using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;

namespace MysteryMud.Application.Commands;

public class GiveCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetPair;

    public void Execute(CommandExecutionContext executionContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount < 2)
        {
            executionContext.Msg.To(actor).Send("Give what to whom ?");
            return;
        }

        ref var inventory = ref actor.Get<Inventory>();
        ref var room = ref actor.Get<Location>().Room;
        ref var roomContents = ref room.Get<RoomContents>();

        // Find target character in room
        var target = EntityFinder.SelectSingleTarget(actor, ctx.Secondary, roomContents.Characters);
        if (target == default)
        {
            executionContext.Msg.To(actor).Send("They are not here.");
            return;
        }

        foreach (var item in EntityFinder.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            // intent to give item
            ref var giveItemIntent = ref executionContext.Intent.GiveItem.Add();
            giveItemIntent.Entity = actor;
            giveItemIntent.Item = item;
            giveItemIntent.Target = target;
        }
    }
}
