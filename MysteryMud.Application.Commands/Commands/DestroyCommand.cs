using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Services;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public sealed class DestroyCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public DestroyCommand(World world, IGameMessageService msg, IIntentWriterContainer intents)
    {
        _world = world;
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            _msg.To(actor).Send("Destroy what ?");
            return;
        }

        // search in inventory (equipped items are also in inventory)
        ref var inventory = ref _world.Get<Inventory>(actor);

        foreach (var item in CommandEntityFinder.SelectTargets(_world, actor, ctx.Primary, inventory.Items))
        {
            // intent to destroy item
            ref var destroyItemIntent = ref _intents.DestroyItem.Add();
            destroyItemIntent.Entity = actor;
            destroyItemIntent.Item = item;
        }
    }
}
