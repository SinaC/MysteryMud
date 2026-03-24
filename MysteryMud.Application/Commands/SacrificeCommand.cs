using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Systems;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;

namespace MysteryMud.Application.Commands;

public class SacrificeCommand : ICommand
{
    public CommandParseOptions ParseOptions => ICommand.Target;

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        // search in room
        ref var room = ref actor.Get<Location>().Room;
        ref var roomContents = ref room.Get<RoomContents>();

        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, roomContents.Items))
        {
            DestroySystem.DestroyItem(item);

            systemContext.MessageBus.Publish(actor, $"You sacrifice the {item.DisplayName}.");
        }
    }
}
