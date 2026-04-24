using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Services;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public sealed class PutCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetPair;

    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public PutCommand(World world, IGameMessageService msg, IIntentWriterContainer intents)
    {
        _world = world;
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount < 2)
        {
            _msg.To(actor).Send("Put what in what ?");
            return;
        }

        ref var inventory = ref _world.Get<Inventory>(actor);

        var container = CommandEntityFinder.FindContainer(_world, actor, ctx.Secondary);
        if (container == null)
        {
            _msg.To(actor).Send("You don't see that here.");
            return;
        }

        foreach (var item in CommandEntityFinder.SelectTargets(_world, actor, ctx.Primary, inventory.Items).Where(x => x != container))
        {
            // intent to put item in container
            ref var putItemIntent = ref _intents.PutItem.Add();
            putItemIntent.Entity = actor;
            putItemIntent.Item = item;
            putItemIntent.Container = container.Value;
        }
    }
}
