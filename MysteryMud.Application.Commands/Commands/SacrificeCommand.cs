using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public sealed class SacrificeCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public SacrificeCommand(World world, IGameMessageService msg, IIntentWriterContainer intents)
    {
        _world = world;
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            _msg.To(actor).Send("Sacrifice what ?");
            return;
        }

        // search in room
        ref var room = ref _world.Get<Location>(actor).Room;
        ref var roomContents = ref _world.Get<RoomContents>(room);

        foreach (var item in CommandEntityFinder.SelectTargets(_world, actor, ctx.Primary, roomContents.Items))
        {
            // intent to sacrifice item
            ref var intent = ref _intents.DestroyItem.Add();
            intent.Entity = actor;
            intent.Item = item;
        }
    }
}
