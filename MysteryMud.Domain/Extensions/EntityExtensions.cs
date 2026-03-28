using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components;
using MysteryMud.Domain.Components.Rooms;

namespace MysteryMud.Domain.Extensions;

public static class EntityExtensions
{
    extension(Entity entity)
    {
        public string Name => entity.Get<Name>().Value;

        public string DisplayName => BuildDisplayName(entity);

        public string DebugName => BuildDebugName(entity);

        private string BuildDisplayName()
        {
            if (!entity.IsAlive())
                return $"DEAD [{entity.Id}]";
            ref var description = ref entity.TryGetRef<Description>(out var descriptionExists);
            if (descriptionExists)
                return description.Value;
            ref var name = ref entity.TryGetRef<Name>(out var nameExists);
            if (nameExists)
                return name.Value;
            return entity.Id.ToString();
        }

        private string BuildDebugName()
        {
            if (!entity.IsAlive())
                return $"DEAD ({entity.Id})";
            ref var name = ref entity.TryGetRef<Name>(out var nameExists);
            if (nameExists)
                return $"{name.Value}[{entity.Id}]";
            return $"[{entity.Id}]";
        }

        private RoomContents? InternalRoomContents()
        {
            ref var location = ref entity.TryGetRef<Location>(out var hasLocation);
            if (!hasLocation)
                return null;
            ref var roomContents = ref location.Room.TryGetRef<RoomContents>(out var hasRoomContents);
            if (!hasRoomContents)
                return null;
            return roomContents;
        }
    }
}
