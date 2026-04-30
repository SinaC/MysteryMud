namespace MysteryMud.GameData.Enums;

public enum FlagModifierKind
{
    Or,        // flags |= FLAG
    Nor,       // flags &= ~FLAG
    Override   // flags = FLAG
}
