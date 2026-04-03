namespace MysteryMud.GameData.Enums;

[Flags]
public enum CommandThrottlingCategories
{
    None = 0,
    Movement = 1 << 0,
    Combat = 1 << 1,
    Social = 1 << 2,
    Utility = 1 << 3,
    Admin = 1 << 4
}
