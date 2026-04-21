using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Groups;
using MysteryMud.Domain.Components.Rooms;

namespace MysteryMud.Domain.Formatters;

public static class ActTargetResolver
{
    public static IEnumerable<Entity> GetGroupTargets(Entity group)
    {
        ref var groupData = ref group.TryGetRef<GroupInstance>(out var isGroup);
        if (!isGroup)
            return [];
        return groupData.Members;
    }

    public static IEnumerable<Entity> GetRoomTargets(Entity actor)
    {
        ref var location = ref actor.TryGetRef<Location>(out var hasLocation);
        if (!hasLocation)
            return [];
        ref var roomContents = ref location.Room.TryGetRef<RoomContents>(out var hasRoomContents);
        if (!hasRoomContents)
            return [];

        return roomContents.Characters.Where(x => x != actor/* && x.Position >= minPosition*/); // TODO: add position check if needed, but it may not be needed for all cases, so maybe add it as an optional parameter to the method
    }

    public static IEnumerable<Entity> GetRoomTargetsExcept(Entity actor, Entity except)
    {
        ref var location = ref actor.TryGetRef<Location>(out var hasLocation);
        if (!hasLocation)
            return [];
        ref var roomContents = ref location.Room.TryGetRef<RoomContents>(out var hasRoomContents);
        if (!hasRoomContents)
            return [];

        return roomContents.Characters.Where(x => x != actor && x != except/* && x.Position >= minPosition*/); // TODO: add position check if needed, but it may not be needed for all cases, so maybe add it as an optional parameter to the method
    }

    public static IEnumerable<Entity> GetAllTargets(Entity actor)
    {
        ref var location = ref actor.TryGetRef<Location>(out var hasLocation);
        if (!hasLocation)
            return [];
        ref var roomContents = ref location.Room.TryGetRef<RoomContents>(out var hasRoomContents);
        if (!hasRoomContents)
            return [];

        return roomContents.Characters; // TODO: add position check if needed, but it may not be needed for all cases, so maybe add it as an optional parameter to the method
    }

    public static IEnumerable<Entity> GetAllTargetsExcept(Entity actor, Entity except)
    {
        ref var location = ref actor.TryGetRef<Location>(out var hasLocation);
        if (!hasLocation)
            return [];
        ref var roomContents = ref location.Room.TryGetRef<RoomContents>(out var hasRoomContents);
        if (!hasRoomContents)
            return [];

        return roomContents.Characters.Where(x => x != except); // TODO: add position check if needed, but it may not be needed for all cases, so maybe add it as an optional parameter to the method
    }
}
