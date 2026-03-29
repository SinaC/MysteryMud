using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct HealEvent
{
    public Entity Target;
    public Entity Source;
    public int Amount;
    public HealSourceTypes SourceType;
}
