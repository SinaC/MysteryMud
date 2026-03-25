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

    public static readonly CommandParseOptions None = new(0, false);
    public static readonly CommandParseOptions FullText = new(0, true);
    public static readonly CommandParseOptions Target = new(1, false);
    public static readonly CommandParseOptions TargetAndText = new(1, true);
    public static readonly CommandParseOptions TargetPair = new(2, false);
    public static readonly CommandParseOptions TargetPairAndText = new(2, true);
    public static readonly CommandParseOptions TargetTriple = new(3, false);
}
