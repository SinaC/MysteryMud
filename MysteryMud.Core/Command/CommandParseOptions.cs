namespace MysteryMud.Core.Command;

public readonly struct CommandParseOptions
{
    public readonly int ArgumentCount { get; init; }
    public readonly bool LastIsText { get; init; }

    public CommandParseOptions(int argumentCount, bool lastIsText)
    {
        ArgumentCount = argumentCount;
        LastIsText = lastIsText;
    }
}
