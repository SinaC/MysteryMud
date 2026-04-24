using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct HealedEvent
{
    public EntityId Target;
    public EntityId Source;
    public int Amount;
    public HealSourceKind SourceKind;
}
