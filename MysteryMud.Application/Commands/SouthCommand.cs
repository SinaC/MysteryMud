using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.OldSystems;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class SouthCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.None;
    public CommandDefinition Definition { get; }

    public SouthCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        // Get room
        ref var room = ref actor.Get<Location>().Room;

        // Get south exit
        var roomGraph = room.Get<RoomGraph>();
        var southExit = roomGraph.Exits.SingleOrDefault(e => e.Direction == Directions.South);
        if (southExit.Equals(default(Exit)) || southExit.TargetRoom == Entity.Null)
        {
            systemContext.Msg.To(actor).Send("Alas, you cannot go that way.");
            return;
        }

        MovementSystem.Move(systemContext, actor, southExit.TargetRoom, Directions.South);
        DisplayRoomSystem.DisplayRoom(systemContext, actor, southExit.TargetRoom);
    }
}
