using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Core.Effects;

public struct DamageAction
{
    public EntityId Target;
    public EntityId Source;
    public decimal Amount;
    public DamageKind DamageKind;
    public DamageSourceKind SourceKind;
}
