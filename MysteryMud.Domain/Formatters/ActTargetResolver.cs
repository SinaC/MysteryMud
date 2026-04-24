using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Components.Rooms;
using TinyECS;

namespace MysteryMud.Domain.Formatters;

public static class ActTargetResolver
{
    public static IEnumerable<EntityId> GetGroupTargets(World world, EntityId group)
    {
        ref var groupData = ref world.TryGetRef<GroupInstance>(group, out var isGroup);
        if (!isGroup)
            return [];
        return groupData.Members;
    }

    public static IEnumerable<EntityId> GetRoomTargets(World world, EntityId actor)
    {
        ref var location = ref world.TryGetRef<Location>(actor, out var hasLocation);
        if (!hasLocation)
            return [];
        ref var roomContents = ref world.TryGetRef<RoomContents>(location.Room, out var hasRoomContents);
        if (!hasRoomContents)
            return [];

        return roomContents.Characters.Where(x => x != actor/* && x.Position >= minPosition*/); // TODO: add position check if needed, but it may not be needed for all cases, so maybe add it as an optional parameter to the method
    }

    public static IEnumerable<EntityId> GetRoomTargetsExcept(World world, EntityId actor, EntityId except)
    {
        ref var location = ref world.TryGetRef<Location>(actor, out var hasLocation);
        if (!hasLocation)
            return [];
        ref var roomContents = ref world.TryGetRef<RoomContents>(location.Room, out var hasRoomContents);
        if (!hasRoomContents)
            return [];

        return roomContents.Characters.Where(x => x != actor && x != except/* && x.Position >= minPosition*/); // TODO: add position check if needed, but it may not be needed for all cases, so maybe add it as an optional parameter to the method
    }

    public static IEnumerable<EntityId> GetAllTargets(World world, EntityId actor)
    {
        ref var location = ref world.TryGetRef<Location>(actor, out var hasLocation);
        if (!hasLocation)
            return [];
        ref var roomContents = ref world.TryGetRef<RoomContents>(location.Room, out var hasRoomContents);
        if (!hasRoomContents)
            return [];

        return roomContents.Characters; // TODO: add position check if needed, but it may not be needed for all cases, so maybe add it as an optional parameter to the method
    }

    public static IEnumerable<EntityId> GetAllTargetsExcept(World world, EntityId actor, EntityId except)
    {
        ref var location = ref world.TryGetRef<Location>(actor, out var hasLocation);
        if (!hasLocation)
            return [];
        ref var roomContents = ref world.TryGetRef<RoomContents>(location.Room, out var hasRoomContents);
        if (!hasRoomContents)
            return [];

        return roomContents.Characters.Where(x => x != except); // TODO: add position check if needed, but it may not be needed for all cases, so maybe add it as an optional parameter to the method
    }
}
