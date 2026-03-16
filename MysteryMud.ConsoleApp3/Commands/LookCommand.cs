using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Extensions;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands;

public class LookCommand : ICommand
{
    public CommandParseMode ParseMode => CommandParseMode.TargetPair;

    // arguments:
    //   - no argument
    //      entity
    //          - character/room/item in room: show room overview (description, exits, items, characters)
    //          - item in inventory: show character description and inventory
    //          - item in container: show container description and contents
    //   - one argument (only for character): prioritize character > item in room > item in inventory > item in equipped slots
    // TODO: handle self, all, indexed targets
    public void Execute(Entity actor, CommandContext ctx)
    {
        // No argument: show room/container overview
        if (ctx.Primary.Kind == TargetKind.Single && ctx.Primary.Name.IsEmpty)
        {
            LookAround(actor);
            return;
        }

        // Get room
        var position = actor.Get<Position>();
        var room = position.Room;

        // Get room contents and graph
        var roomContents = room.Get<RoomContents>();
        var roomItems = roomContents.Items;
        var roomCharacters = roomContents.Characters;

        // One argument: prioritize
        var targetName = ctx.Primary.Name;

        // 1️) Try characters in room
        foreach (var c in roomCharacters)
        {
            if (TargetingSystem.Matches(c, targetName))
            {
                MessageSystem.SendMessage(actor, $"Character: {c.DisplayName}");
                return;
            }
        }

        // 2️) Try items in room
        foreach (var item in roomItems)
        {
            if (TargetingSystem.Matches(item, targetName))
            {
                MessageSystem.SendMessage(actor, $"Item: {item.DisplayName}");
                return;
            }
        }

        // 3️) Try items in actor inventory
        if (actor.Has<Inventory>())
        {
            var inv = actor.Get<Inventory>();
            foreach (var item in inv.Items)
            {
                if (TargetingSystem.Matches(item, targetName))
                {
                    MessageSystem.SendMessage(actor, $"You are carrying: {item.DisplayName}");
                    return;
                }
            }
        }

        // 4) Try items in equipped slots (optional)
        // For demo, we assume inventory = equipment too
        MessageSystem.SendMessage(actor, $"You see nothing matching '{ctx.Primary.Name}' here.");
    }

    private void LookAround(Entity actor)
    {
        ref var position = ref actor.TryGetRef<Position>(out var hasPosition);
        if (hasPosition)
        {
            DisplayRoomSystem.DisplayRoom(actor, position.Room);
            return;
        }

        ref var containedIn = ref actor.TryGetRef<ContainedIn>(out var hasContainedIn);
        if (hasContainedIn)
        {
            if (containedIn.Character != Entity.Null)
            {
                MessageSystem.SendMessage(actor, $"You are in {containedIn.Character.DisplayName}'s inventory.");
                // TODO: display inventory ?
                return;
            }
            else if (containedIn.Container != Entity.Null)
            {
                MessageSystem.SendMessage(actor, $"You are inside {containedIn.Container.DisplayName}.");
                // TODO: display container contents ?
                return;
            }
        }
    }
}
