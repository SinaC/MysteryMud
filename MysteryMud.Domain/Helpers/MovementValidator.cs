using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Helpers;

public static class MovementValidator
{
    public static bool CanPayMoveCost(
        World world,
        EntityId mover,
        EntityId fromRoom,
        EntityId toRoom,
        DirectionKind direction,
        out int cost)
    {
        cost = ResolveMoveCost(world, mover, fromRoom, toRoom, direction);
        ref var move = ref world.Get<Move>(mover);
        return cost <= move.Current;
    }

    public static void PayMoveCost(World world, EntityId mover, int cost)
    {
        ref var move = ref world.Get<Move>(mover);
        move.Current -= cost;
    }

    public static int ResolveMoveCost(
        World world,
        EntityId mover,
        EntityId fromRoom,
        EntityId toRoom,
        DirectionKind direction)
    {
        return 10; // TODO: use sector to calculate move cost
    }

    public static bool CanEnter(
        World world,
        EntityId mover,
        EntityId fromRoom,
        EntityId toRoom,
        DirectionKind direction,
        out string blockReason)
    {
        blockReason = default!;

        // 1. Destination room must exist
        if (toRoom == EntityId.Invalid|| !world.IsAlive(toRoom))
        {
            blockReason = "There is no exit in that direction";
            return false;
        }

        // 2. Exit must not be closed
        if (world.Has<RoomGraph>(fromRoom))
        {
            ref readonly var graph = ref world.Get<RoomGraph>(fromRoom);
            if (graph.Exits[direction]?.Closed == true)
            {
                blockReason = "The door is closed";
                return false;
                // TODO
                //blockReason = exit.IsLocked
                //    ? "The door is locked"
                //    : "The door is closed";
                //return false;
            }
        }

        // 3. Mover must not be in combat
        if (world.Has<CombatState>(mover))
        {
            blockReason = "You are fighting!";
            return false;
        }

        // TODO
        //// 4. Mover must not be stunned / incapacitated / sleeping
        //if (world.Has<Stunned>(mover))
        //{
        //    blockReason = "You are stunned";
        //    return false;
        //}

        //if (world.Has<Incapacitated>(mover))
        //{
        //    blockReason = "You are incapacitated";
        //    return false;
        //}

        //if (world.Has<Sleeping>(mover))
        //{
        //    blockReason = "You are sleeping";
        //    return false;
        //}

        // TODO
        //// 5. Sector / room flags
        //if (world.Has<RoomFlags>(toRoom))
        //{
        //    ref readonly var flags = ref world.Get<RoomFlags>(toRoom);

        //    if (flags.Has(RoomFlag.NoMob) && world.Has<NpcMarker>(mover))
        //    {
        //        blockReason = "NPCs cannot enter this area";
        //        return false;
        //    }

        //    if (flags.Has(RoomFlag.Private))
        //    {
        //        // Private rooms: block if already 2+ players inside
        //        // (classic ROM rule — adjust threshold as needed)
        //        int playerCount = CountPlayersInRoom(world, toRoom);
        //        if (playerCount >= 2 && !world.Has<NpcMarker>(mover))
        //        {
        //            blockReason = "That room is private";
        //            return false;
        //        }
        //    }
        //}

        // TODO
        //// 6. Water/fly sector checks
        //if (world.Has<SectorKind>(toRoom))
        //{
        //    ref readonly var sector = ref world.Get<SectorKind>(toRoom);

        //    bool canSwim = world.Has<CanSwim>(mover)
        //                || world.Has<WaterBreathing>(mover);
        //    bool canFly = world.Has<Flying>(mover);

        //    if (sector.Value == Sector.WaterNoSwim && !canFly && !canSwim)
        //    {
        //        blockReason = "You need to be able to swim to go there";
        //        return false;
        //    }

        //    if (sector.Value == Sector.Air && !canFly)
        //    {
        //        blockReason = "You need to be flying to go there";
        //        return false;
        //    }
        //}

        // TODO: move cost

        return true;
    }

    public static bool CanFlee(
        World world,
        EntityId fleer,
        EntityId fromRoom,
        EntityId toRoom,
        DirectionKind direction)
    {
        // TODO: same as CanEnter without checking combat
        return true;
    }

    // ------------------------------------------------------------------
    private static int CountPlayersInRoom(World world, EntityId room)
    {
        ref var roomContents = ref world.Get<RoomContents>(room);
        return roomContents.Characters.Count;
    }
}
