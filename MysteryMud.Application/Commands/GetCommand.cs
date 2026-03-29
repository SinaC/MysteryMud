using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class GetCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.TargetPair;
    public CommandDefinition Definition { get; }

    public GetCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.To(actor).Send("Get what ?");
            return;
        }

        if (ctx.Secondary.Name.IsEmpty)
        {
            // default: room
            var room = actor.Get<Location>().Room;
            var roomContents = room.Get<RoomContents>();
            foreach (var item in EntityFinder.SelectTargets(actor, ctx.Primary, roomContents.Items))
            {
                // intent to get item from room
                ref var getItemFromRoomIntent = ref systemContext.Intent.GetItem.Add();
                getItemFromRoomIntent.Entity = actor;
                getItemFromRoomIntent.Item = item;
                getItemFromRoomIntent.SourceKind = GetSourceKind.Room;
                getItemFromRoomIntent.Source = room;
            }
        }
        else
        {
            var container = EntityFinder.FindContainer(actor, ctx.Secondary);
            if (container == default)
            {
                systemContext.Msg.To(actor).Send("You don't see that here.");
                return;
            }

            var containerContents = container.Get<ContainerContents>();
            foreach (var item in EntityFinder.SelectTargets(actor, ctx.Primary, containerContents.Items))
            {
                // intent to get item from container
                ref var getItemFromContainerIntent = ref systemContext.Intent.GetItem.Add();
                getItemFromContainerIntent.Entity = actor;
                getItemFromContainerIntent.Item = item;
                getItemFromContainerIntent.SourceKind = GetSourceKind.Container;
                getItemFromContainerIntent.Source = container;
            }
        }
    }
}
