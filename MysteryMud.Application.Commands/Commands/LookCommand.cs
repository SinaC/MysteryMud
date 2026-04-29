using DefaultEcs;
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

namespace MysteryMud.Application.Commands.Commands;

public sealed class LookCommand : ICommand
{
    private static CommandParseOptions ParseOptions { get; } = CommandParseOptions.TargetPair;

    private readonly IGameMessageService _msg;
    private readonly IIntentWriterContainer _intents;

    public LookCommand(IGameMessageService msg, IIntentWriterContainer intents)
    {
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
    public void Execute(GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args)
    {
        CommandParser.Parse(cmd, args, ParseOptions.ArgumentCount, ParseOptions.LastIsText, out var ctx);

        // No argument: show room/container overview
        if (ctx.TargetCount == 0)
        {
            if (!actor.Has<Location>())
            {
                _msg.To(actor).Send("You are floating in the void. You see nothing.");
                return;
            }
            ref var location = ref actor.Get<Location>();

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
        ref var room = ref actor.Get<Location>().Room;

        // Get room contents and graph
        ref var roomContents = ref room.Get<RoomContents>();
        var roomItems = roomContents.Items;
        var roomCharacters = roomContents.Characters;

        // One argument: prioritize
        var targetName = ctx.Primary.Name;

        // 1️) Try characters in room
        var target = CommandEntityFinder.SelectSingleTarget(actor, ctx.Primary, roomCharacters);
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
        var item = CommandEntityFinder.SelectSingleTarget(actor, ctx.Primary, roomItems);
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
        if (actor.Has<Inventory>())
        {
            ref var inventory = ref actor.Get<Inventory>();
            var inventoryItem = CommandEntityFinder.SelectSingleTarget(actor, ctx.Primary, inventory.Items);
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
