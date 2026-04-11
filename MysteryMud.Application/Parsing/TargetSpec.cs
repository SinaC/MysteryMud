using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Targeting;

public ref struct TargetSpec
{
    public TargetKind Kind; // Single, All, Indexed, Self
    public int Index; // for N.something
    public ReadOnlySpan<char> Name; // empty for 'all' or 'self'
}
