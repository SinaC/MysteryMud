using Arch.Core;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class SayCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.FullText;
    public CommandDefinition Definition { get; }

    public SayCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, CommandContext ctx)
    {
        if (ctx.Text.IsEmpty)
        {
            systemContext.Msg.To(actor).Send("Say what?");
            return;
        }

        systemContext.Msg.ToAll(actor).Act($"{0} say{0:v}: {1}").With(actor, ctx.Text.ToString());
    }
}
