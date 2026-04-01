using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class SacrificeCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    public CommandDefinition Definition { get; }

    public SacrificeCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.To(actor).Send("Sacrifice what ?");
            return;
        }

        // search in room
        ref var room = ref actor.Get<Location>().Room;
        ref var roomContents = ref room.Get<RoomContents>();

        foreach (var item in EntityFinder.SelectTargets(actor, ctx.Primary, roomContents.Items))
        {
            // intent to sacrifice item
            ref var intent = ref systemContext.Intent.DestroyItem.Add();
            intent.Entity = actor;
            intent.Item = item;
        }
    }
}
