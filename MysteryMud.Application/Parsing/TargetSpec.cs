namespace MysteryMud.Application.Parsing;

public ref struct TargetSpec
{
    public TargetKind Kind;
    public int Index; // for N.something
    public ReadOnlySpan<char> Name; // empty for 'all' or 'self'
}
