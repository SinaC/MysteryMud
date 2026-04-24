using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Contracts;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Application.Commands.Commands;

public sealed class LookCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetPair;

    private readonly World _world;
    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public LookCommand(World world, IGameMessageService msg, IIntentWriterContainer intents)
    {
        _world = world;
        _msg = msg;
        _intents = intents;
    }

    // arguments:
    //   - no argument
    //      entity
    //          - character/room/item in room: show room overview (description, exits, items, characters)
    //          - item in inventory: show character description and inventory
    //          - item in container: show container description and contents
    //   - one argument (only for character): prioritize character > item in room > item in inventory > item in equipped slots
    // TODO: handle self, all, indexed targets
    public void Execute(GameState state, EntityId actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        // No argument: show room/container overview
        if (ctx.TargetCount == 0)
        {
            ref var location = ref _world.TryGetRef<Location>(actor, out var hasLocation);
            if (!hasLocation)
            {
                _msg.To(actor).Send("You are floating in the void. You see nothing.");
                return;
            }

            // intent to look at room
            ref var lookRoomIntent = ref _intents.Look.Add();
            lookRoomIntent.Viewer = actor;
            lookRoomIntent.TargetKind = LookTargetKind.Room;
            lookRoomIntent.Target = location.Room;
            return;
        }

        // TODO:
        // if inside a container or inventory, look around that first

        // Get room
        ref var room = ref _world.Get<Location>(actor).Room;

        // Get room contents and graph
        ref var roomContents = ref _world.Get<RoomContents>(room);
        var roomItems = roomContents.Items;
        var roomCharacters = roomContents.Characters;

        // One argument: prioritize
        var targetName = ctx.Primary.Name;

        // 1️) Try characters in room
        var target = CommandEntityFinder.SelectSingleTarget(_world, actor, ctx.Primary, roomCharacters);
        if (target != null)
        {
            // intent to look at character
            ref var lookCharacterIntent = ref _intents.Look.Add();
            lookCharacterIntent.Viewer = actor;
            lookCharacterIntent.TargetKind = LookTargetKind.Character;
            lookCharacterIntent.Target = target.Value;
            return;
        }

        // 2️) Try items in room
        var item = CommandEntityFinder.SelectSingleTarget(_world, actor, ctx.Primary, roomItems);
        if (item != null)
        {
            // intent to look at item
            ref var lookItemIntent = ref _intents.Look.Add();
            lookItemIntent.Viewer = actor;
            lookItemIntent.TargetKind = LookTargetKind.Item;
            lookItemIntent.Target = item.Value;
            return;
        }
        
        // 3️) Try items in actor inventory
        ref var inventory = ref _world.TryGetRef<Inventory>(actor, out var hasInventory);
        if (hasInventory)
        {
            var inventoryItem = CommandEntityFinder.SelectSingleTarget(_world, actor, ctx.Primary, inventory.Items);
            if (inventoryItem != null)
            {
                // intent to look at item
                ref var lookItemIntent = ref _intents.Look.Add();
                lookItemIntent.Viewer = actor;
                lookItemIntent.TargetKind = LookTargetKind.Item;
                lookItemIntent.Target = inventoryItem.Value;
                return;
            }
        }

        _msg.To(actor).Send($"You don't see that here.");
    }
}
