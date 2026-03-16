using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class SayCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.FullText;

    public void Execute(Entity actor, CommandContext ctx)
    {
        if (ctx.Text.IsEmpty)
        {
            MessageSystem.SendMessage(actor, "Say what?");
            return;
        }

        // Get room
        var position = actor.Get<Position>();
        var room = position.Room;

        // Get room contents
        var roomContents = room.Get<RoomContents>();
        var roomCharacters = roomContents.Characters;

        MessageSystem.SendMessage(actor, $"You say: {ctx.Text}");
        foreach (var target in roomCharacters)
        {
            if (!target.Equals(actor))
            {
                MessageSystem.SendMessage(actor, $"{actor.DisplayName} says: {ctx.Text}");
            }
        }
    }
}
