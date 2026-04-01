using Arch.Core;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries.Matching;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.Commands;

public class TellCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetAndText;
    public CommandDefinition Definition { get; }

    public TellCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    public void Execute(SystemContext systemContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            systemContext.Msg.To(actor).Send("Tell whom?");
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
                systemContext.Msg.To([actor, target]).Act("{0} tell{0:v} {1}: {2}").With(actor, target, message);
                found = true;
            }
        });

        if (found)
            return;

        systemContext.Msg.To(actor).Send("They aren't here.");
    }
}
