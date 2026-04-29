using DefaultEcs;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Components.Rooms;
using MysteryMud.Domain.Helpers;

namespace MysteryMud.Domain.Formatters;

public static class ActTargetResolver
{
    public static IEnumerable<Entity> GetGroupTargets(Entity group)
    {
        if (!GroupHelpers.IsAlive(group) || !group.Has<GroupInstance>())
            return [];
        ref var groupInstance = ref group.Get<GroupInstance>();
        return groupInstance.Members;
    }

    public static IEnumerable<Entity> GetRoomTargets(Entity actor)
    {
        if (!actor.Has<Location>())
            return [];
        ref var location = ref actor.Get<Location>();
        if (!location.Room.Has<RoomContents>())
            return [];
        ref var roomContents = ref location.Room.Get<RoomContents>();
        return roomContents.Characters.Where(x => x != actor/* && x.Position >= minPosition*/); // TODO: add position check if needed, but it may not be needed for all cases, so maybe add it as an optional parameter to the method
    }

    public static IEnumerable<Entity> GetRoomTargetsExcept(Entity actor, Entity except)
    {
        if (!actor.Has<Location>())
            return [];
        ref var location = ref actor.Get<Location>();
        if (!location.Room.Has<RoomContents>())
            return [];
        ref var roomContents = ref location.Room.Get<RoomContents>();
        return roomContents.Characters.Where(x => x != actor && x != except/* && x.Position >= minPosition*/); // TODO: add position check if needed, but it may not be needed for all cases, so maybe add it as an optional parameter to the method
    }

    public static IEnumerable<Entity> GetAllTargets(Entity actor)
    {
        if (!actor.Has<Location>())
            return [];
        ref var location = ref actor.Get<Location>();
        if (!location.Room.Has<RoomContents>())
            return [];
        ref var roomContents = ref location.Room.Get<RoomContents>();
        return roomContents.Characters; // TODO: add position check if needed, but it may not be needed for all cases, so maybe add it as an optional parameter to the method
    }

    public static IEnumerable<Entity> GetAllTargetsExcept(Entity actor, Entity except)
    {
        if (!actor.Has<Location>())
            return [];
        ref var location = ref actor.Get<Location>();
        if (!location.Room.Has<RoomContents>())
            return [];
        ref var roomContents = ref location.Room.Get<RoomContents>();
        return roomContents.Characters.Where(x => x != except); // TODO: add position check if needed, but it may not be needed for all cases, so maybe add it as an optional parameter to the method
    }
}
