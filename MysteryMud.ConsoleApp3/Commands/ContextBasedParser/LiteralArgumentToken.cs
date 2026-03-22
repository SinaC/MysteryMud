using MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

public class LiteralArgumentToken : ISyntaxToken
{
    public readonly string _literal; // TODO: change
    public readonly string _argName; // TODO: change
    private readonly ArgKind _kind;
    public readonly ArgValue _rawValue;

    public LiteralArgumentToken(string literal, string argName, ArgKind kind, ArgValue rawValue)
    {
        _literal = literal;
        _argName = argName;
        _kind = kind;
        _rawValue = rawValue;
    }
}