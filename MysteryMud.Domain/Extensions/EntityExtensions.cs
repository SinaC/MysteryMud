using DefaultEcs;
using MysteryMud.Domain.Components;

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
            if (!entity.IsAlive)
                return $"DEAD [{entity.GetHashCode()}]";
            if (entity.Has<Description>())
                return entity.Get<Description>().Value;
            if (entity.Has<Name>())
                return entity.Get<Name>().Value;
            return entity.GetHashCode().ToString();
        }

        private string BuildDebugName()
        {
            if (!entity.IsAlive)
                return $"DEAD ({entity.GetHashCode()})";
            if (entity.Has<Name>())
                return $"{entity.Get<Name>().Value}[{entity.GetHashCode()}]";
            return $"[{entity.GetHashCode()}]";
        }
    }
}
