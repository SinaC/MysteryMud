namespace MysteryMud.GameData.Enums;

// What kinds of entities are eligible.
[Flags]
public enum AbilityTargetFilter
{
    None = 0,
    Player = 1 << 0,
    NPC = 1 << 1,
    Item = 1 << 2,

    // Player | NPC
    Character = Player | NPC,

    // Player | NPC | Item
    Any = Player | NPC | Item,
}
