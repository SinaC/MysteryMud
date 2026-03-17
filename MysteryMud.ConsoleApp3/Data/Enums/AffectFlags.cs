namespace MysteryMud.ConsoleApp3.Data.Enums;

[Flags]
public enum AffectFlags : ulong
{
    None = 0,
    Poison = 1 << 0,
    Haste = 1 << 1,
    Sanctuary = 1 << 2,
    Bless = 1 << 3
}
