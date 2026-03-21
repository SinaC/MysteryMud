namespace MysteryMud.ConsoleApp3.Commands.v2;

public interface ISyntaxToken
{
    //bool Match(string token, Dictionary<string, object> args);
    bool Match(ReadOnlySpan<char> input, Dictionary<string, object> args);
}
