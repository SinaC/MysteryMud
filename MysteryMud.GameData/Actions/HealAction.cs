using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Actions;

public struct HealAction
{
    public Entity Target;
    public Entity Source;
    public int Amount;
    public HealSourceTypes SourceType;
}
