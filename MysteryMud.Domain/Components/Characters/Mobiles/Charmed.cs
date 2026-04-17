using Arch.Core;

namespace MysteryMud.Domain.Components.Characters.Mobiles;

public struct Charmed
{
    public Entity Master;         // back-reference
}
