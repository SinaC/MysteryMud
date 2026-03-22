using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Items;
using MysteryMud.ConsoleApp3.Components.Rooms;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

public static class ParameterResolver // GameParameterResolver 
{
    public static bool TryResolve(
        CommandContext ctx,
        ResolutionContext res,
        ArgumentToken arg,
        ReadOnlySpan<char> rawInput,
        out ArgValue value)
    {
        value = default;

        if (rawInput.IsEmpty)
            return false;

        switch (arg.Kind)
        {
            case ArgKind.Amount:
                if (int.TryParse(rawInput, out int amount))
                {
                    value = new ArgValue { Type = ArgValue.ArgType.Int, IntValue = amount };
                    return true;
                }
                break;

            case ArgKind.Player:
                value = TryResolvePlayer(ctx, rawInput);
                return value.Type != ArgValue.ArgType.Failed;

            case ArgKind.Container:
                value = TryResolveContainer(ctx, res, rawInput);
                return value.Type != ArgValue.ArgType.Failed;

            case ArgKind.Item:
                // Try to resolve item inside container or inventory
                value = TryResolveItem(ctx, res, arg, rawInput);
                return value.Type != ArgValue.ArgType.Failed;

            case ArgKind.String:
                value = new ArgValue { Type = ArgValue.ArgType.String, StringValue = rawInput.ToString() };
                return true;
        }

        value = default;
        return false;
    }

    private static ArgValue TryResolveContainer(
        CommandContext ctx,
        ResolutionContext res,
        ReadOnlySpan<char> rawInput)
    {
        // check inventory
        var inv = GetInventory(ctx.Actor);
        var match = FindByName(inv, rawInput);

        if (match != Entity.Null)
        {
            return new ArgValue { Type = ArgValue.ArgType.Entity, EntityValue = match};
        }

        // check room
        var roomItems = GetRoomItems(ctx.Actor);
        match = FindByName(roomItems, rawInput);

        if (match != Entity.Null)
        {
            return new ArgValue { Type = ArgValue.ArgType.Entity, EntityValue = match };
        }

        // not found
        return ArgValue.Failed(rawInput);
    }

    private static ArgValue TryResolveItem(
        CommandContext ctx,
        ResolutionContext res,
        ArgumentToken arg,
        ReadOnlySpan<char> rawInput)
    {
        // use Scope to determine where to look for items first (container, inventory, room)
        var itemArgParseResult = ItemArgParser.TryParse(rawInput, out var itemArg);
        if (!itemArgParseResult)
            return ArgValue.Failed(rawInput);

        List<Entity> candidates = [];
        switch (arg.Scope)
        {
            case ArgScope.Room: return SearchRoomContents(ctx.Actor, itemArg, rawInput);
            case ArgScope.Inventory: return SearchPlayerInventory(ctx.Actor, itemArg, rawInput);
            case ArgScope.ContainerOnly:
                if (res.Args.TryGetValue("container", out var container))
                    return GetContainerContents(container.EntityValue, itemArg, rawInput);
                else
                    return ArgValue.Failed(rawInput);
            case ArgScope.InventoryThenRoom: return SearchPlayerInventoryThenRoom(ctx.Actor, itemArg, rawInput);
            case ArgScope.RoomThenInventory: return SearchRoomThenInventory(ctx.Actor, itemArg, rawInput);
        }

        // Not found
        return ArgValue.Failed(rawInput);
    }

    private static ArgValue GetContainerContents(Entity container, ItemArg itemArg, ReadOnlySpan<char> rawInput)
    {
        ref var content = ref container.TryGetRef<ContainerContents>(out var isContainer);
        if (!isContainer)
            return ArgValue.Failed(rawInput);
        var matches = MatchItems(content.Items, itemArg);
        if (matches.Count > 0)
            return new ArgValue { Type = ArgValue.ArgType.EntityCollection, Entities = matches };
        return ArgValue.Failed(rawInput);
    }

    private static ArgValue SearchPlayerInventory(Entity actor, ItemArg itemArg, ReadOnlySpan<char> rawInput)
    {
        var inventory = GetInventory(actor);
        var inventoryMatches = MatchItems(inventory, itemArg);
        if (inventoryMatches.Count > 0)
            return new ArgValue { Type = ArgValue.ArgType.EntityCollection, Entities = inventoryMatches };
        return ArgValue.Failed(rawInput);
    }

    private static ArgValue SearchRoomThenInventory(Entity actor, ItemArg itemArg, ReadOnlySpan<char> rawInput)
    {
        var roomItems = GetRoomItems(actor);
        var roomMatches = MatchItems(roomItems, itemArg);
        if (roomMatches.Count > 0)
            return new ArgValue { Type = ArgValue.ArgType.EntityCollection, Entities = roomMatches };
        var inventory = GetInventory(actor);
        var inventoryMatches = MatchItems(inventory, itemArg);
        if (inventoryMatches.Count > 0)
            return new ArgValue { Type = ArgValue.ArgType.EntityCollection, Entities = inventoryMatches };
        return ArgValue.Failed(rawInput);
    }

    private static ArgValue SearchRoomContents(Entity actor, ItemArg itemArg, ReadOnlySpan<char> rawInput)
    {
        var roomItems = GetRoomItems(actor);
        var roomMatches = MatchItems(roomItems, itemArg);
        if (roomMatches.Count > 0)
            return new ArgValue { Type = ArgValue.ArgType.EntityCollection, Entities = roomMatches };
        return ArgValue.Failed(rawInput);
    }

    private static ArgValue SearchPlayerInventoryThenRoom(Entity actor, ItemArg itemArg, ReadOnlySpan<char> rawInput)
    {
        var inventory = GetInventory(actor);
        var inventoryMatches = MatchItems(inventory, itemArg);
        if (inventoryMatches.Count > 0)
            return new ArgValue { Type = ArgValue.ArgType.EntityCollection, Entities = inventoryMatches };
        var roomItems = GetRoomItems(actor);
        var roomMatches = MatchItems(roomItems, itemArg);
        if (roomMatches.Count > 0)
            return new ArgValue { Type = ArgValue.ArgType.EntityCollection, Entities = roomMatches };
        return ArgValue.Failed(rawInput);
    }

    private static ArgValue TryResolvePlayer(CommandContext ctx, ReadOnlySpan<char> rawInput)
    {
        var entity = FindPlayer(ctx.Actor, rawInput);
        if (entity != Entity.Null)
            return new ArgValue { Type = ArgValue.ArgType.Entity, EntityValue = entity };
        return ArgValue.Failed(rawInput);
    }

    private static Entity FindByName(IEnumerable<Entity> entities, ReadOnlySpan<char> name)
    {
        foreach (var e in entities)
        {
            if (NameSystem.Matches(e, name))
                return e;
        }
        return Entity.Null;
    }

    private static Entity FindPlayer(Entity actor, ReadOnlySpan<char> name)
    {
        ref var position = ref actor.TryGetRef<Position>(out var hasPosition);
        if (!hasPosition)
            return Entity.Null;
        ref var roomContent = ref position.Room.TryGetRef<RoomContents>(out var hasRoomContent);
        if (!hasRoomContent)
            return Entity.Null;
        return FindByName(roomContent.Characters, name);
    }

    private static List<Entity> GetInventory(Entity entity)
    {
        ref var inventory = ref entity.TryGetRef<Inventory>(out var hasInventory);
        if (!hasInventory)
            return [];
        return inventory.Items;
    }

    private static List<Entity> GetRoomItems(Entity entity)
    {
        ref var position = ref entity.TryGetRef<Position>(out var hasPosition);
        if (!hasPosition)
            return [];
        ref var roomContent = ref position.Room.TryGetRef<RoomContents>(out var hasRoomContent);
        if (!hasRoomContent)
            return [];
        return roomContent.Items;
    }

    private static List<Entity> MatchItems(IEnumerable<Entity> source, ItemArg arg)
    {
        // TODO: see TargetingSystem.SelectTargets which avoid call NameSystem.Matches on all entities
        if (arg.All)
            return source.ToList();
        var matches = source
            .Where(e => NameSystem.Matches(e, arg.Name))
            .ToList();
        if (arg.AllOf)
            return matches;
        if (arg.Index.HasValue)
        {
            if (arg.Index.Value - 1 < matches.Count)
            {
                return [matches[arg.Index.Value - 1]];
            }
            return [];
        }
        // default: first match
        return matches.Take(1).ToList();
    }

    // TODO: handle this
    //            "item" => new ItemParser(),
    //            "item1" => new ItemParser(),
    //            "item2" => new ItemParser(),
    //            "container" => new ContainerParser(),
    //            "container1" => new ContainerParser(),
    //            "container2" => new ContainerParser(),
    //            "min" => new IntParser(),
    //            "max" => new IntParser(),
    //            "count" => new IntParser(),
    //            "pet" => new StringParser(),
    //            "name" => new StringParser(),
    //            "player" => new StringParser(),
    //            "amount" => new IntParser(),
    //public static bool TryResolve(
    //    CommandContext ctx,
    //    ResolutionContext resolutionContext,
    //    string argName,
    //    ArgKind argKind,
    //    ReadOnlySpan<char> token,
    //    out ArgValue value)
    //{
    //    // ===== ITEM =====
    //    if (argName == "item" || argName.Contains("item"))
    //    {
    //        if (!ItemArgParser.TryParse(token, out var itemArg))
    //        {
    //            value = default;
    //            return false;
    //        }

    //        // resolve ECS entities
    //        var entities = ResolveItems(ctx.Actor, itemArg);

    //        if (entities.Count == 0)
    //        {
    //            value = default;
    //            return false;
    //        }

    //        value = ArgValue.FromItem(itemArg, entities);
    //        return true;
    //    }

    //    // TODO: container

    //    // ===== AMOUNT =====
    //    if (argName == "amount")
    //    {
    //        if (int.TryParse(token, out int i))
    //        {
    //            value = ArgValue.FromInt(i);
    //            return true;
    //        }
    //        value = default;
    //        return false;
    //    }

    //    // ===== PLAYER =====
    //    if (argName == "player" || argName == "character")
    //    {
    //        var name = token.ToString();
    //        var entity = FindPlayer(ctx.Actor, name);

    //        if (entity == Entity.Null)
    //        {
    //            value = default;
    //            return false;
    //        }

    //        value = ArgValue.FromEntity(entity);
    //        return true;
    //    }

    //    // ===== DEFAULT STRING =====
    //    value = ArgValue.FromString(token.ToString());
    //    return true;
    //}

    //private static List<Entity> ResolveItems(Entity actor, ItemArg arg)
    //{
    //    // Example logic:
    //    ref var inventory = ref actor.TryGetRef<Inventory>(out var hasInventory);
    //    if (!hasInventory)
    //        return [];

    //    // TODO: see TargetingSystem.SelectTargets which avoid call NameSystem.Matches on all entities
    //    var matches = inventory.Items
    //        .Where(i => NameSystem.Matches(i, arg.Name))
    //        .ToList();

    //    if (arg.All)
    //        return matches;

    //    if (arg.Index.HasValue)
    //    {
    //        if (arg.Index.Value - 1 < matches.Count)
    //        {
    //            return [matches[arg.Index.Value - 1]];
    //        }

    //        return [];
    //    }

    //    // default: first match
    //    return matches.Take(1).Select(i => i).ToList();
    //}

    //private static Entity FindPlayer(Entity actor, string name) => Entity.Null;
}