using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Application.Commands;

public class LookCommand : ICommand
{
    public CommandParseOptions ParseOptions => CommandParseOptions.TargetPair;
    public CommandDefinition Definition { get; }

    public LookCommand(CommandDefinition definition)
    {
        Definition = definition;
    }

    // arguments:
    //   - no argument
    //      entity
    //          - character/room/item in room: show room overview (description, exits, items, characters)
    //          - item in inventory: show character description and inventory
    //          - item in container: show container description and contents
    //   - one argument (only for character): prioritize character > item in room > item in inventory > item in equipped slots
    // TODO: handle self, all, indexed targets
    public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx)
    {
        // No argument: show room/container overview
        if (ctx.TargetCount == 0)
        {
            ref var location = ref actor.TryGetRef<Location>(out var hasLocation);
            if (!hasLocation)
            {
                systemContext.Msg.To(actor).Send("You are floating in the void. You see nothing.");
                return;
            }

            // intent to look at room
            ref var lookRoomIntent = ref systemContext.Intent.Look.Add();
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
        foreach (var target in roomCharacters)
        {
            if (EntityFinder.Matches(target, targetName))
            {
                // intent to look at character
                ref var lookCharacterIntent = ref systemContext.Intent.Look.Add();
                lookCharacterIntent.Viewer = actor;
                lookCharacterIntent.TargetKind = LookTargetKind.Character;
                lookCharacterIntent.Target = target;
                return;
            }
        }

        // 2️) Try items in room
        foreach (var item in roomItems)
        {
            if (EntityFinder.Matches(item, targetName))
            {
                // intent to look at item
                ref var lookItemIntent = ref systemContext.Intent.Look.Add();
                lookItemIntent.Viewer = actor;
                lookItemIntent.TargetKind = LookTargetKind.Item;
                lookItemIntent.Target = item;
                return;
            }
        }

        // 3️) Try items in actor inventory
        ref var inventory = ref actor.TryGetRef<Inventory>(out var hasInventory);
        if (hasInventory)
        {
            foreach (var item in inventory.Items)
            {
                if (EntityFinder.Matches(item, targetName))
                {
                    // intent to look at item
                    ref var lookItemIntent = ref systemContext.Intent.Look.Add();
                    lookItemIntent.Viewer = actor;
                    lookItemIntent.TargetKind = LookTargetKind.Item;
                    lookItemIntent.Target = item;
                    return;
                }
            }
        }

        // 4) Try items in equipped slots (optional)
        // For demo, we assume inventory = equipment too
        systemContext.Msg.To(actor).Send($"You see nothing matching '{ctx.Primary.Name}' here.");
    }
}
