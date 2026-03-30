using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct DamagedEvent
{
    public Entity Target;
    public Entity Source;
    public int Amount;
    public DamageKind DamageKind;
    public DamageSourceKind SourceKind;
}
