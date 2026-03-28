namespace MysteryMud.Domain.OldSystems;

// TODO
public static class VisibilitySystem
{
    //static void EquipItem(World world, Entity actor, Entity item)
    //{
    //    ref var eq = ref world.Get<Equipment>(actor);

    //    eq.Slots[EquipSlot.Weapon] = item;

    //    world.Get<StatsDirty>(actor).Value = true;

    //    if (world.Has<LightSource>(item))
    //    {
    //        world.Add(actor, world.Get<LightSource>(item));

    //        Console.WriteLine("You light the torch.");
    //    }
    //}

    //    static bool HasLight(World world, Entity room)
    //    {
    //        var entities = world.Get<RoomEntities>(room).Entities;

    //        foreach (var e in entities)
    //        {
    //            if (world.Has<LightSource>(e))
    //                return true;
    //        }

    //        return false;
    //    }

    //    public static bool CanSee(World world, Entity viewer, Entity target)
    //    {
    //        if (!world.IsAlive(target))
    //            return false;

    //        var room = world.Get<InRoom>(viewer).Room;

    //        bool roomDark = world.Has<DarkRoom>(room);
    //        bool roomLit = HasLight(world, room);

    //        if (roomDark && !roomLit && !world.Has<LightSource>(viewer))
    //            return false;

    //        if (world.Has<Invisible>(target))
    //        {
    //            if (!world.Has<DetectInvisible>(viewer))
    //                return false;
    //        }

    //        if (world.Has<Hidden>(target))
    //        {
    //            if (!world.Has<DetectHidden>(viewer))
    //            {
    //                int perception = world.Has<Perception>(viewer)
    //                    ? world.Get<Perception>(viewer).Value
    //                    : 0;

    //                int difficulty = world.Get<Hidden>(target).Difficulty;

    //                if (perception < difficulty)
    //                    return false;
    //            }
    //        }

    //        return true;
    //    }
}
