using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct DamagedEvent
{
    public EntityId Target;
    public EntityId Source;
    public int Amount;
    public DamageKind DamageKind;
    public DamageSourceKind SourceKind;
}
