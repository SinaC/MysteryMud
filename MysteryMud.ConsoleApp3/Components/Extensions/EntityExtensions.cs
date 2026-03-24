using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;

namespace MysteryMud.ConsoleApp3.Components.Extensions;

public static class EntityExtensions
{
    extension(Entity entity)
    {
        public string Name => entity.Get<Name>().Value;

        public string DisplayName => BuildDisplayName(entity);

        public string DebugName => BuildDebugName(entity);

        private string BuildDisplayName()
        {
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
            ref var name = ref entity.TryGetRef<Name>(out var nameExists);
            if (nameExists)
                return $"{name.Value}({entity.Id} {entity.IsAlive()})";
            return $"({entity.Id} {entity.IsAlive()})";
        }
    }
}
