namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

public class ArgumentToken : ISyntaxToken
{
    public string Name;
    public ArgKind Kind;
    public ArgScope Scope;

    public bool AllowAll;        // supports "all"
    public bool AllowAllOf;      // supports "all.sword"
    public bool AllowIndex;      // supports "5.sword"
}