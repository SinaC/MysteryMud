using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.OldSystems;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class TellCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.TargetAndText;
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
            systemContext.Msg.To(actor).Send("Tell whom?");
            return;
        }

        var roomContents = actor.Get<Location>().Room.Get<RoomContents>().Characters;
        foreach (var target in TargetingSystem.SelectTargets(actor, ctx.Primary, roomContents))
        {
            systemContext.Msg.To([actor, target]).Act("{0} tell{0:v} {1}: {2}").With(actor, target, message.ToString());
        }
    }
}
