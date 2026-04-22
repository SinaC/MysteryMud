using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands;

public sealed class SacrificeCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public SacrificeCommand(IGameMessageService msg, IIntentWriterContainer intents)
    {
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            _msg.To(actor).Send("Sacrifice what ?");
            return;
        }

        // search in room
        ref var room = ref actor.Get<Location>().Room;
        ref var roomContents = ref room.Get<RoomContents>();

        foreach (var item in CommandEntityFinder.SelectTargets(actor, ctx.Primary, roomContents.Items))
        {
            // intent to sacrifice item
            ref var intent = ref _intents.DestroyItem.Add();
            intent.Entity = actor;
            intent.Item = item;
        }
    }
}
