using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.OldSystems;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class SacrificeCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.Target;
    public CommandDefinition Definition { get; }

    public SacrificeCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.To(actor).Send("Sacrifice what ?");
            return;
        }

        // search in room
        ref var room = ref actor.Get<Location>().Room;
        ref var roomContents = ref room.Get<RoomContents>();

        foreach (var item in TargetingSystem.SelectTargets(actor, ctx.Primary, roomContents.Items))
        {
            DestroySystem.DestroyItem(item);

            systemContext.Msg.To(actor).Send($"You sacrifice the {item.DisplayName}.");
        }
    }
}
