using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Application.Parsing;
using MysteryMud.Application.Queries;
using MysteryMud.Core;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Items;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.OldSystems;
using MysteryMud.GameData.Definitions;

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
            LookAround(systemContext, actor);
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
                systemContext.Msg.To(actor).Send($"Character: {target.DisplayName}");

                // TODO: remove, should be in ExamineCommand
                ref var targetInventory = ref target.TryGetRef<Inventory>(out var hasTargetInventory);
                if (hasTargetInventory)
                {
                    foreach (var item in targetInventory.Items)
                    {
                        systemContext.Msg.To(actor).Act("{0} {0:b} carrying: {1}").With(target, item);
                    }
                }
                return;
            }
        }

        // 2️) Try items in room
        foreach (var item in roomItems)
        {
            if (EntityFinder.Matches(item, targetName))
            {
                systemContext.Msg.To(actor).Send($"Item: {item.DisplayName}");

                // TODO: remove, should be in ExamineCommand
                ref var containerContents = ref item.TryGetRef<ContainerContents>(out var isContainerContents);
                if (isContainerContents)
                {
                    foreach (var containerItem in containerContents.Items)
                    {
                        systemContext.Msg.To(actor).Send($"It contains: {containerItem.DisplayName}");
                    }
                }
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
                    systemContext.Msg.To(actor).Send($"You are carrying: {item.DisplayName}");
                    return;
                }
            }
        }

        // 4) Try items in equipped slots (optional)
        // For demo, we assume inventory = equipment too
        systemContext.Msg.To(actor).Send($"You see nothing matching '{ctx.Primary.Name}' here.");
    }

    private void LookAround(SystemContext systemContext, Entity actor)
    {
        ref var location = ref actor.TryGetRef<Location>(out var hasLocation);
        if (hasLocation)
        {
            DisplayRoomSystem.DisplayRoom(systemContext, actor, location.Room);
            return;
        }

        ref var containedIn = ref actor.TryGetRef<ContainedIn>(out var hasContainedIn);
        if (hasContainedIn)
        {
            if (containedIn.Character != Entity.Null)
            {
                systemContext.Msg.To(actor).Send($"You are in {containedIn.Character.DisplayName}'s inventory.");
                // TODO: display inventory ?
                return;
            }
            else if (containedIn.Container != Entity.Null)
            {
                systemContext.Msg.To(actor).Send($"You are inside {containedIn.Container.DisplayName}.");
                // TODO: display container contents ?
                return;
            }
        }
    }
}
