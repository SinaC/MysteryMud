namespace MysteryMud.Application.Parsing;

public interface ICommandParser
{
    void Parse(ReadOnlySpan<char> cmd, ReadOnlySpan<char> args, int argumentCount, bool lastIsText, out CommandContext ctx);
    void SplitCommand(ReadOnlySpan<char> input, out ReadOnlySpan<char> command, out ReadOnlySpan<char> args);
}
