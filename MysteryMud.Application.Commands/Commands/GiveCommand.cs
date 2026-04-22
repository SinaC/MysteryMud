using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands;

public sealed class GiveCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetPair;

    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public GiveCommand(IGameMessageService msg, IIntentWriterContainer intents)
    {
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount < 2)
        {
            _msg.To(actor).Send("Give what to whom ?");
            return;
        }

        ref var inventory = ref actor.Get<Inventory>();
        ref var room = ref actor.Get<Location>().Room;
        ref var roomContents = ref room.Get<RoomContents>();

        // Find target character in room
        var target = CommandEntityFinder.SelectSingleTarget(actor, ctx.Secondary, roomContents.Characters);
        if (target == null)
        {
            _msg.To(actor).Send("They are not here.");
            return;
        }

        foreach (var item in CommandEntityFinder.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            // intent to give item
            ref var giveItemIntent = ref _intents.GiveItem.Add();
            giveItemIntent.Entity = actor;
            giveItemIntent.Item = item;
            giveItemIntent.Target = target.Value;
        }
    }
}
