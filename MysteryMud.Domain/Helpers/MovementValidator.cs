using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Helpers;

public static class MovementValidator
{
    public static bool CanPayMoveCost(
        Entity mover,
        Entity fromRoom,
        Entity toRoom,
        DirectionKind direction,
        out int cost)
    {
        cost = ResolveMoveCost(mover, fromRoom, toRoom, direction);
        ref var move = ref mover.Get<Move>();
        return cost <= move.Current;
    }

    public static void PayMoveCost(Entity mover, int cost)
    {
        ref var move = ref mover.Get<Move>();
        move.Current -= cost;
    }

    public static int ResolveMoveCost(
        Entity mover,
        Entity fromRoom,
        Entity toRoom,
        DirectionKind direction)
    {
        return 10; // TODO: use sector to calculate move cost
    }

    public static bool CanEnter(
        Entity mover,
        Entity fromRoom,
        Entity toRoom,
        DirectionKind direction,
        out string blockReason)
    {
        blockReason = default!;

        // 1. Destination room must exist
        if (toRoom == Entity.Null || !toRoom.IsAlive())
        {
            blockReason = "There is no exit in that direction";
            return false;
        }

        // 2. Exit must not be closed
        if (fromRoom.Has<RoomGraph>())
        {
            ref readonly var graph = ref fromRoom.Get<RoomGraph>();
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
        if (mover.Has<CombatState>())
        {
            blockReason = "You are fighting!";
            return false;
        }

        // TODO
        //// 4. Mover must not be stunned / incapacitated / sleeping
        //if (mover.Has<Stunned>())
        //{
        //    blockReason = "You are stunned";
        //    return false;
        //}

        //if (mover.Has<Incapacitated>())
        //{
        //    blockReason = "You are incapacitated";
        //    return false;
        //}

        //if (mover.Has<Sleeping>())
        //{
        //    blockReason = "You are sleeping";
        //    return false;
        //}

        // TODO
        //// 5. Sector / room flags
        //if (toRoom.Has<RoomFlags>())
        //{
        //    ref readonly var flags = ref toRoom.Get<RoomFlags>();

        //    if (flags.Has(RoomFlag.NoMob) && mover.Has<NpcMarker>())
        //    {
        //        blockReason = "NPCs cannot enter this area";
        //        return false;
        //    }

        //    if (flags.Has(RoomFlag.Private))
        //    {
        //        // Private rooms: block if already 2+ players inside
        //        // (classic ROM rule — adjust threshold as needed)
        //        int playerCount = CountPlayersInRoom(toRoom);
        //        if (playerCount >= 2 && !mover.Has<NpcMarker>())
        //        {
        //            blockReason = "That room is private";
        //            return false;
        //        }
        //    }
        //}

        // TODO
        //// 6. Water/fly sector checks
        //if (toRoom.Has<SectorKind>())
        //{
        //    ref readonly var sector = ref toRoom.Get<SectorKind>();

        //    bool canSwim = mover.Has<CanSwim>()
        //                || mover.Has<WaterBreathing>();
        //    bool canFly = mover.Has<Flying>();

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
        Entity fleer,
        Entity fromRoom,
        Entity toRoom,
        DirectionKind direction)
    {
        // TODO: same as CanEnter without checking combat
        return true;
    }

    // ------------------------------------------------------------------
    private static int CountPlayersInRoom(Entity room)
    {
        ref var roomContents = ref room.Get<RoomContents>();
        return roomContents.Characters.Count;
    }
}
