using Arch.Core;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands;

public sealed class GtellCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.FullText;

    private readonly IGameMessageService _msg;

    public GtellCommand(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.Text.IsEmpty)
        {
            _msg.To(actor).Send("Gtell what?");
            return;
        }

        _msg.ToAll(actor).Act("%g{0} say{0:v} the group: {1}%x").With(actor, ctx.Text.ToString());
    }
}
