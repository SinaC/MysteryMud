namespace MysteryMud.GameData.Enums;

public enum AutoFlags
{
    None = 0,
    Assist = 1 << 0,
    Loot = 1 << 1,
    Sacrifice = 1 << 2,
}
