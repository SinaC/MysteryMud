using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Systems;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class TellCommand : ICommand
{
    public CommandParseOptions ParseOptions => ICommand.TargetAndText;
    public CommandDefinition Definition { get; }

    public TellCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        var message = ctx.Text;

        if (ctx.TargetCount == 0)
        {
            systemContext.MessageBus.Publish(actor, "Tell whom?");
            return;
        }

        var roomContents = actor.Get<Location>().Room.Get<RoomContents>().Characters;
        foreach (var target in TargetingSystem.SelectTargets(actor, ctx.Primary, roomContents))
        {
            systemContext.MessageBus.Publish(actor, $"You tell {target.DisplayName}: {message}");
            systemContext.MessageBus.Publish(target, $"{actor.DisplayName} tells you: {message}");
        }
    }
}
