using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Commands.Parser;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Core.Eventing;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class SayCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.FullText;

    public void Execute(GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.Text.IsEmpty)
        {
            MessageBus.Publish(actor, "Say what?");
            return;
        }

        // Get room
        var room = actor.Get<Location>().Room;

        // Get room contents
        var roomContents = room.Get<RoomContents>();
        var roomCharacters = roomContents.Characters;

        MessageBus.Publish(actor, $"You say: {ctx.Text}");
        foreach (var target in roomCharacters)
        {
            if (!target.Equals(actor))
            {
                MessageBus.Publish(actor, $"{actor.DisplayName} says: {ctx.Text}");
            }
        }
    }
}
