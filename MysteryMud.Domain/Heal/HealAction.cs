using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Heal;

public struct HealAction
{
    public Entity Target;
    public Entity Source;
    public int Amount;
    public HealSourceKind SourceKind;
}
