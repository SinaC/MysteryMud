using TinyECS;

namespace MysteryMud.Domain.Components.Items;

public struct ContainedIn
{
    public EntityId Character;
    public EntityId Container;
}
