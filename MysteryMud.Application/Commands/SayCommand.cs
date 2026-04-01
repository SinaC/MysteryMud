using Arch.Core;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class SayCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.FullText;

    public CommandDefinition Definition { get; }

    public SayCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.Text.IsEmpty)
        {
            systemContext.Msg.To(actor).Send("Say what?");
            return;
        }

        systemContext.Msg.ToAll(actor).Act("{0} say{0:v}: {1}").With(actor, ctx.Text.ToString());
    }
}
