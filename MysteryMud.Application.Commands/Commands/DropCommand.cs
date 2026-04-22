using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands;

public sealed class DropCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public DropCommand(IGameMessageService msg, IIntentWriterContainer intents)
    {
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            _msg.To(actor).Send("Drop what ?");
            return;
        }

        ref var inventory = ref actor.Get<Inventory>();
        ref var room = ref actor.Get<Location>().Room;

        foreach (var item in CommandEntityFinder.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            // intent to drop item
            ref var dropItemIntent = ref _intents.DropItem.Add();
            dropItemIntent.Entity = actor;
            dropItemIntent.Item = item;
            dropItemIntent.Room = room;
        }
    }
}
