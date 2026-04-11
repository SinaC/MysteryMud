using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Core.Effects;

public struct DamageAction
{
    public Entity Target;
    public Entity Source;
    public decimal Amount;
    public DamageKind DamageKind;
    public DamageSourceKind SourceKind;
}
