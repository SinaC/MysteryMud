using Arch.Core;

namespace MysteryMud.ConsoleApp3.Systems;

public static class VisibilitySystem
{
    public static bool CanSee(Entity viewer, Entity target)
    {
        //// Check if viewer is blind
        //if (viewer.Has<Blindness>()) return false;
        //// Check if target is invisible and viewer can't see invisible
        //if (target.Has<Invisibility>() && !viewer.Has<SeeInvisible>()) return false;
        //// Check if target is in a dark room and viewer can't see in the dark
        //var targetRoom = target.Get<Position>().Room;
        //if (targetRoom.Has<Darkness>() && !viewer.Has<NightVision>()) return false;
        //// Otherwise, visible
        return true;
    }
}
