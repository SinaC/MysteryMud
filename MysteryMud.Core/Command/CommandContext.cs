namespace MysteryMud.Core.Command;

public ref struct CommandContext
{
    public ReadOnlySpan<char> Command;
    public TargetSpec Primary;
    public TargetSpec Secondary;
    public ReadOnlySpan<char> Text;
}
