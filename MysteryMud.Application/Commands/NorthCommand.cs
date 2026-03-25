using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class NorthCommand : ICommand
{
    public CommandParseOptions ParseOptions => ICommand.None;
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
        var northExit = roomGraph.Exits.SingleOrDefault(e => e.Direction == Direction.North);
        if (northExit.Equals(default(Exit)) || northExit.TargetRoom == Entity.Null)
        {
            systemContext.MessageBus.Publish(actor, "Alas, you cannot go that way.");
            return;
        }

        systemContext.MessageBus.Publish(actor, $"You leaves north."); // TODO send message to current room: "{actor} leaves North."
        MovementSystem.Move(actor, northExit.TargetRoom);
        DisplayRoomSystem.DisplayRoom(systemContext, actor, northExit.TargetRoom);
        // TODO: send message to target room: "{actor} has arrived."
    }
}
