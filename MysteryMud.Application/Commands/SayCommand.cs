using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class SayCommand : ICommand
{
    public CommandParseOptions ParseOptions => ICommand.FullText;
    public CommandDefinition Definition { get; }

    public SayCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        if (ctx.Text.IsEmpty)
        {
            systemContext.MessageBus.Publish(actor, "Say what?");
            return;
        }

        // Get room
        var room = actor.Get<Location>().Room;

        // Get room contents
        var roomContents = room.Get<RoomContents>();
        var roomCharacters = roomContents.Characters;

        systemContext.MessageBus.Publish(actor, $"You say: {ctx.Text}");
        foreach (var target in roomCharacters)
        {
            if (!target.Equals(actor))
            {
                systemContext.MessageBus.Publish(target, $"{actor.DisplayName} says: {ctx.Text}");
            }
        }
    }
}
