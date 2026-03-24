namespace MysteryMud.Domain.Data.Enums;

public enum ModifierType
{
    Flat,       // STR +5
    AddPercent, // STR +20%
    Multiply,   // STR * 1.5
    Override    // STR = 10
}
