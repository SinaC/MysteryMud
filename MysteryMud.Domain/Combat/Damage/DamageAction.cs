using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Combat.Damage;

public struct DamageAction
{
    public Entity Target;
    public Entity Source;
    public int Amount;
    public DamageKind DamageKind;
    public DamageSourceKind SourceKind;
}
