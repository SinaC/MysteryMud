namespace MysteryMud.GameData.Enums;

[Flags]
public enum AbilityTargetKindMask : ushort
{
    None = 0,

    // Character domain
    Self = 1 << 0,
    Ally = 1 << 1,
    Enemy = 1 << 2,
    Neutral = 1 << 3,
    Player = 1 << 4,
    NPC = 1 << 5,

    // Item domain
    Inventory = 1 << 6,
    Equipped = 1 << 7,
    Ground = 1 << 8,

    // Useful composites
    AnyCharacter = Self | Ally | Enemy | Neutral | Player | NPC,
    AnyItem = Inventory | Equipped | Ground,
    Any = AnyCharacter | AnyItem
}
