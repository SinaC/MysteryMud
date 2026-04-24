using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Services;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public sealed class RemoveCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public RemoveCommand(World world, IGameMessageService msg, IIntentWriterContainer intents)
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
            _msg.To(actor).Send("Remove what ?");
            return;
        }

        ref var equipment = ref _world.Get<Equipment>(actor);

        foreach (var kv in equipment.Slots)
        {
            var slot = kv.Key;
            var item = kv.Value;

            if (Domain.Queries.EntityFinder.Matches(_world, item, ctx.Primary.Name))
            {
                // intent to remove item
                ref var removeItemIntent = ref _intents.RemoveItem.Add();
                removeItemIntent.Entity = actor;
                removeItemIntent.Item = item;
                removeItemIntent.Slot = slot;
            }
        }
    }
}
