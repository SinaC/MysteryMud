namespace MysteryMud.Core.Command;

public ref struct CommandContext
{
    public ReadOnlySpan<char> Command;
    public TargetSpec Primary;
    public TargetSpec Secondary;
    public TargetSpec Tertiary;
    public TargetSpec Quaternary;
    public TargetSpec Quinary; // max 5 targets
    public ReadOnlySpan<char> Text; // optional free-form text argument, usually the last argument in the command
}
