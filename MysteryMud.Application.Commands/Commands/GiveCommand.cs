using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public sealed class GiveCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetPair;

    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public GiveCommand(World world, IGameMessageService msg, IIntentWriterContainer intents)
    {
        _world = world;
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount < 2)
        {
            _msg.To(actor).Send("Give what to whom ?");
            return;
        }

        ref var inventory = ref _world.Get<Inventory>(actor);
        ref var room = ref _world.Get<Location>(actor).Room;
        ref var roomContents = ref _world.Get<RoomContents>(room);

        // Find target character in room
        var target = CommandEntityFinder.SelectSingleTarget(_world, actor, ctx.Secondary, roomContents.Characters);
        if (target == null)
        {
            _msg.To(actor).Send("They are not here.");
            return;
        }

        foreach (var item in CommandEntityFinder.SelectTargets(_world, actor, ctx.Primary, inventory.Items))
        {
            // intent to give item
            ref var giveItemIntent = ref _intents.GiveItem.Add();
            giveItemIntent.Entity = actor;
            giveItemIntent.Item = item;
            giveItemIntent.Target = target.Value;
        }
    }
}
