namespace MysteryMud.GameData.Enums;

[Flags]
public enum AssistFlags
{
    None = 0,
    GuardPlayers = 1 << 0,  // guards helping lowbies
    SameRace = 1 << 1,
    SameClass = 1 << 2,
    SameAlign = 1 << 3,
    SameFaction = 1 << 4,
}
