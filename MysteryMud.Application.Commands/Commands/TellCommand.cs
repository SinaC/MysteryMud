using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters.Players;
using MysteryMud.Domain.Queries.Matching;
using MysteryMud.Domain.Services;
using TinyECS;
using TinyECS.Extensions;

namespace MysteryMud.Application.Commands.Commands;

public sealed class TellCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetAndText;

    private readonly World _world;
    private readonly IGameMessageService _msg;

    public TellCommand(World world, IGameMessageService msg)
    {
        _world = world;
        _msg = msg;
    }

    private static readonly QueryDescription _playersQueryDesc = new QueryDescription()
        .WithAll<Name, PlayerTag>();

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
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

        _world.Query(_playersQueryDesc, (EntityId target,
            ref Name _, ref PlayerTag _) =>
        {
            if (NameMatcher.Matches(_world, target, primaryName))
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
