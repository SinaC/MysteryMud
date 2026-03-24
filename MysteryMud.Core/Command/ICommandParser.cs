namespace MysteryMud.Core.Command;

public interface ICommandParser
{
    void Parse(CommandParseMode parseMode, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args, out CommandContext ctx);
    void SplitCommand(ReadOnlySpan<char> input, out ReadOnlySpan<char> command, out ReadOnlySpan<char> args);
}
