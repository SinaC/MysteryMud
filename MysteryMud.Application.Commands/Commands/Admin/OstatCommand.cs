using DefaultEcs;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Application.Services;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;

namespace MysteryMud.Application.Commands.Commands.Admin;

public sealed class OstatCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly IGameMessageService _msg;
    private readonly IEffectDisplayService _effectDisplayService;

    public OstatCommand(IGameMessageService msg, IEffectDisplayService effectDisplayService)
    {
        _msg = msg;
        _effectDisplayService = effectDisplayService;
    }

    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        if (ctx.TargetCount == 0)
        {
            _msg.To(actor).Send("Ostat what ?");
            return;
        }

        var target = SearchTarget(actor, ref ctx.Primary);
        if (target == null)
        {
            _msg.To(actor).Send("No such target.");
            return;
        }

        ref var name = ref target.Value.Get<Name>();
        ref var itemEffects = ref target.Value.Get<ItemEffects>();
        _msg.To(actor).Send($"Name: {name.Value}");
        if (target.Value.Has<Description>())
            _msg.To(actor).Send($"Description: {target.Value.Get<Description>().Value}");
        _msg.To(actor).Send($"Active tags: {itemEffects.Data.ActiveTags}");
        _effectDisplayService.DisplayEffects(state, actor, itemEffects.Data.Effects);
    }

    private Entity? SearchTarget(Entity actor, ref TargetSpec targetSpec)
    {
        // 1) Try items in actor inventory
        if (actor.Has<Inventory>())
        {
            ref var inventory = ref actor.Get<Inventory>();
            var inventoryItem = CommandEntityFinder.SelectSingleTarget(actor, targetSpec, inventory.Items);
            if (inventoryItem is not null)
                return inventoryItem.Value;
        }

        // 2️) Try items in room
        var roomItems = actor.Get<Location>().Room.Get<RoomContents>().Items;
        var roomItem = CommandEntityFinder.SelectSingleTarget(actor, targetSpec, roomItems);
        if (roomItem is not null)
            return roomItem.Value;
        return null;
    }
}
