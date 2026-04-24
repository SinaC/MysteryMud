using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Helpers;
using MysteryMud.Domain.Services;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public sealed class KillCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly World _world;
    private readonly IGameMessageService _msg;

    public KillCommand(World world, IGameMessageService msg)
    {
        _world = world;
        _msg = msg;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            _msg.To(actor).Send("Kill whom ?");
            return;
        }

        ref var room = ref _world.Get<Location>(actor).Room;
        ref var people = ref _world.Get<RoomContents>(room).Characters;
        var target = CommandEntityFinder.SelectSingleTarget(_world, actor, ctx.Primary, people);

        if (target == null)
        {
            _msg.To(actor).Send("They aren't here.");
            return;
        }

        if (target.Equals(actor))
        {
            _msg.To(actor).Send("You hit yourself. Ouch.");
            return;
        }

        if (_world.Has<CombatState>(actor))
        {
            _msg.To(actor).Send("You do the best you can!");
            return;
        }

        // TODO: check if target already in combat, if so, maybe switch targets? Or maybe not allow switching targets?

        // flag both as in combat with each other, with the target striking back after a delay
        CombatHelpers.EnterCombat(_world, state, actor, target.Value);
    }
}
