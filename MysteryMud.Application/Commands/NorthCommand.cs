using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.OldSystems;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class NorthCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.None;
    public CommandDefinition Definition { get; }

    public NorthCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        // Get room
        ref var room = ref actor.Get<Location>().Room;

        // Get North exit
        ref var roomGraph = ref room.Get<RoomGraph>();
        var northExit = roomGraph.Exits.SingleOrDefault(e => e.Direction == Directions.North);
        if (northExit.Equals(default(Exit)) || northExit.TargetRoom == Entity.Null)
        {
            systemContext.Msg.To(actor).Send("Alas, you cannot go that way.");
            return;
        }

        MovementSystem.Move(systemContext, actor, northExit.TargetRoom, Directions.North);
        DisplayRoomSystem.DisplayRoom(systemContext, actor, northExit.TargetRoom);
    }
}
