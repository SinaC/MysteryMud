using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Intent;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Components.Characters;

namespace MysteryMud.Application.Commands;

public class DestroyCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public DestroyCommand(IGameMessageService msg, IIntentWriterContainer intents)
    {
        _msg = msg;
        _intents = intents;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            _msg.To(actor).Send("Destroy what ?");
            return;
        }

        // search in inventory (equipped items are also in inventory)
        ref var inventory = ref actor.Get<Inventory>();

        foreach (var item in EntityFinder.SelectTargets(actor, ctx.Primary, inventory.Items))
        {
            // intent to destroy item
            ref var destroyItemIntent = ref _intents.DestroyItem.Add();
            destroyItemIntent.Entity = actor;
            destroyItemIntent.Item = item;
        }
    }
}
