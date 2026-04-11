using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Core.Effects;

public struct HealAction
{
    public Entity Target;
    public Entity Source;
    public decimal Amount;
    public HealSourceKind SourceKind;
}
