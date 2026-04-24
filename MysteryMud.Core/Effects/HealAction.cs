using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Core.Effects;

public struct HealAction
{
    public EntityId Target;
    public EntityId Source;
    public decimal Amount;
    public HealSourceKind SourceKind;
}
