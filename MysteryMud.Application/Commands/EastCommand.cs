using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class EastCommand : ICommand
{
    public CommandDefinition Definition { get; }

    public EastCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        if (actor.Has<CombatState>())
        {
            systemContext.Msg.To(actor).Send("No way! You are still fighting!");
            return;
        }

        // Get room
        ref var room = ref actor.Get<Location>().Room;

        // Get east exit
        ref var roomGraph = ref room.Get<RoomGraph>();
        var eastExit = roomGraph.Exits.SingleOrDefault(e => e.Direction == DirectionKind.East);
        if (eastExit.Equals(default(Exit)) || eastExit.TargetRoom == Entity.Null)
        {
            systemContext.Msg.To(actor).Send("Alas, you cannot go that way.");
            return;
        }

        // intent to move
        ref var moveIntent = ref systemContext.Intent.Move.Add();
        moveIntent.Actor = actor;
        moveIntent.FromRoom = room;
        moveIntent.ToRoom = eastExit.TargetRoom;
        moveIntent.Direction = DirectionKind.East;
        moveIntent.AutoLook = true;
    }
}
