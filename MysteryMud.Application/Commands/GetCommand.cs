using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class GetCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetPair;

    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public GetCommand(IGameMessageService msg, IIntentWriterContainer intents)
    {
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
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
            ref var room = ref actor.Get<Location>().Room;
            ref var roomContents = ref room.Get<RoomContents>();
            foreach (var item in EntityFinder.SelectTargets(actor, ctx.Primary, roomContents.Items))
            {
                // intent to get item from room
                ref var getItemFromRoomIntent = ref _intents.GetItem.Add();
                getItemFromRoomIntent.Entity = actor;
                getItemFromRoomIntent.Item = item;
                getItemFromRoomIntent.SourceKind = GetSourceKind.Room;
                getItemFromRoomIntent.Source = room;
            }
        }
        else
        {
            var container = EntityFinder.FindContainer(actor, ctx.Secondary);
            if (container == null)
            {
                _msg.To(actor).Send("You don't see that here.");
                return;
            }

            ref var containerContents = ref container.Value.Get<ContainerContents>();
            foreach (var item in EntityFinder.SelectTargets(actor, ctx.Primary, containerContents.Items))
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
}
