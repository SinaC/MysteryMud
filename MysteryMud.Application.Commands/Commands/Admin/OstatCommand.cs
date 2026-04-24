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
using TinyECS;

namespace MysteryMud.Application.Commands.Commands.Admin;

public sealed class OstatCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.Target;

    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IEffectDisplayService _effectDisplayService;

    public OstatCommand(World world, IGameMessageService msg, IEffectDisplayService effectDisplayService)
    {
        _world = world;
        _msg = msg;
        _effectDisplayService = effectDisplayService;
    }

    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
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

        ref var name = ref _world.Get<Name>(target.Value);
        ref var itemEffects = ref _world.Get<ItemEffects>(target.Value);
        _msg.To(actor).Send($"Name: {name.Value}");
        ref var description = ref _world.TryGetRef<Description>(target.Value, out var hasDescription);
        if (hasDescription)
            _msg.To(actor).Send($"Description: {description.Value}");
        _msg.To(actor).Send($"Active tags: {itemEffects.Data.ActiveTags}");
        _effectDisplayService.DisplayEffects(state, actor, itemEffects.Data.Effects);
    }

    private EntityId? SearchTarget(EntityId actor, ref TargetSpec targetSpec)
    {
        // 1) Try items in actor inventory
        ref var inventory = ref _world.TryGetRef<Inventory>(actor, out var hasInventory);
        if (hasInventory)
        {
            var inventoryItem = CommandEntityFinder.SelectSingleTarget(_world, actor, targetSpec, inventory.Items);
            if (inventoryItem is not null)
                return inventoryItem.Value;
        }

        // 2️) Try items in room
        ref var room = ref _world.Get<Location>(actor).Room;
        ref var roomItems = ref _world.Get<RoomContents>(room).Items;
        var roomItem = CommandEntityFinder.SelectSingleTarget(_world, actor, targetSpec, roomItems);
        if (roomItem is not null)
            return roomItem.Value;
        return null;
    }
}
