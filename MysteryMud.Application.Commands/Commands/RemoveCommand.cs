using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands;

public class RemoveCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public RemoveCommand(IGameMessageService msg, IIntentWriterContainer intents)
    {
        _msg = msg;
        _intents = intents;
    }
    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            _msg.To(actor).Send("Remove what ?");
            return;
        }

        ref var equipment = ref actor.Get<Equipment>();

        foreach (var kv in equipment.Slots)
        {
            var slot = kv.Key;
            var item = kv.Value;

            if (Domain.Queries.EntityFinder.Matches(item, ctx.Primary.Name))
            {
                // intent to remove item
                ref var removeItemIntent = ref _intents.RemoveItem.Add();
                removeItemIntent.Actor = actor;
                removeItemIntent.Item = item;
                removeItemIntent.Slot = slot;
            }
        }
    }
}
