using TinyECS;

namespace MysteryMud.Domain.Components.Characters.Mobiles;

public struct Charmed
{
    public EntityId Master;         // back-reference
}
