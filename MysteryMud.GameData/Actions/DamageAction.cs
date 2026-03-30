using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Actions;

public struct DamageAction
{
    public Entity Target;
    public Entity Source;
    public int Amount;
    public DamageTypes DamageType;
    public DamageSourceTypes SourceType;
}
