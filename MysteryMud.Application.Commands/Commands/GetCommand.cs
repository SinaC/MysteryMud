using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public sealed class GetCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetPair;

    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public GetCommand(World world, IGameMessageService msg, IIntentWriterContainer intents)
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
            _msg.To(actor).Send("Get what ?");
            return;
        }

        if (ctx.Secondary.Name.IsEmpty)
        {
            // default: room
            ref var room = ref _world.Get<Location>(actor).Room;
            ref var roomContents = ref _world.Get<RoomContents>(room);
            var found = false;
            foreach (var item in CommandEntityFinder.SelectTargets(_world, actor, ctx.Primary, roomContents.Items))
            {
                // intent to get item from room
                ref var getItemFromRoomIntent = ref _intents.GetItem.Add();
                getItemFromRoomIntent.Entity = actor;
                getItemFromRoomIntent.Item = item;
                getItemFromRoomIntent.SourceKind = GetSourceKind.Room;
                getItemFromRoomIntent.Source = room;
                found = true;
            }
            if (!found)
                _msg.To(actor).Send("You don't see that here.");
            return;
        }

        var container = CommandEntityFinder.FindContainer(_world, actor, ctx.Secondary);
        if (container == null)
        {
            _msg.To(actor).Send("You don't see that here.");
            return;
        }

        ref var containerContents = ref _world.Get<ContainerContents>(container.Value);
        foreach (var item in CommandEntityFinder.SelectTargets(_world, actor, ctx.Primary, containerContents.Items))
        {
            // intent to get item from container
            ref var getItemFromContainerIntent = ref _intents.GetItem.Add();
            getItemFromContainerIntent.Entity = actor;
            getItemFromContainerIntent.Item = item;
            getItemFromContainerIntent.SourceKind = GetSourceKind.Container;
            getItemFromContainerIntent.Source = container.Value;
        }
    }
}
