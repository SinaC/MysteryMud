using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Services;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public sealed class DropCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public DropCommand(World world, IGameMessageService msg, IIntentWriterContainer intents)
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
            _msg.To(actor).Send("Drop what ?");
            return;
        }

        ref var inventory = ref _world.Get<Inventory>(actor);
        ref var room = ref _world.Get<Location>(actor).Room;

        foreach (var item in CommandEntityFinder.SelectTargets(_world, actor, ctx.Primary, inventory.Items))
        {
            // intent to drop item
            ref var dropItemIntent = ref _intents.DropItem.Add();
            dropItemIntent.Entity = actor;
            dropItemIntent.Item = item;
            dropItemIntent.Room = room;
        }
    }
}
