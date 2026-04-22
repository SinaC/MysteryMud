using MysteryMud.GameData.Attributes;

namespace MysteryMud.GameData.Enums;

public enum DirectionKind
{
    North = 0,
    South = 1,
    East = 2,
    West = 3,
    [EnumSentinel] Count, // always last, used for array sizing
}
