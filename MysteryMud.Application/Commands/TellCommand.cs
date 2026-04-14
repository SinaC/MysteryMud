using Arch.Core;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Queries.Matching;

namespace MysteryMud.Application.Commands;

public class TellCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetAndText;

    private readonly IGameMessageService _msg;

    public TellCommand(IGameMessageService msg)
    {
        _msg = msg;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            _msg.To(actor).Send("Tell whom?");
            return;
        }

        var found = false;
        var message = ctx.Text.ToString();
        var primaryName = ctx.Primary.Name.ToString();

        var query = new QueryDescription().WithAll<Name, PlayerTag>();
        state.World.Query(query, (Entity target, ref Name _, ref PlayerTag _) =>
        {
            if (NameMatcher.Matches(target, primaryName))
            {
                _msg.To([actor, target]).Act("{0} tell{0:v} {1}: {2}").With(actor, target, message);
                found = true;
            }
        });

        if (found)
            return;

        _msg.To(actor).Send("They aren't here.");
    }
}
