using Arch.Core;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands;

public class SayCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.FullText;

    private readonly IGameMessageService _msg;

    public SayCommand(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.Text.IsEmpty)
        {
            _msg.To(actor).Send("Say what?");
            return;
        }

        _msg.ToAll(actor).Act("{0} say{0:v}: {1}").With(actor, ctx.Text.ToString());
    }
}
