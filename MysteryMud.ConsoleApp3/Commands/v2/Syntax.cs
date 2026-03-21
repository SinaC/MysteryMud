namespace MysteryMud.ConsoleApp3.Commands.v2;

public class Syntax
{
    public string Pattern { get; }
    private List<ISyntaxToken> Tokens { get; }

    public Syntax(string pattern)
    {
        Pattern = pattern;
        Tokens = pattern.Split(' ')
                        .Select(t =>
                        {
                            ISyntaxToken token;
                            if (t.StartsWith("[") && t.EndsWith("*]"))
                                token = new GreedyArgumentToken(t[1..^2]);
                            else if (t.StartsWith("[") && t.EndsWith("]"))
                                token = new ArgumentToken(t[1..^1]);
                            else if (t.Equals("silver", StringComparison.OrdinalIgnoreCase) || t.Equals("gold", StringComparison.OrdinalIgnoreCase))
                                return new LiteralArgumentToken(t, "currency"); // capture as argument
                            else
                                token = new WordToken(t);
                            return token;
                        })
                        .ToList();
    }

    //public bool TryMatch(string[] inputTokens, out Dictionary<string, object> args)
    //{
    //    args = new Dictionary<string, object>();
    //    if (inputTokens.Length != Tokens.Count) return false;

    //    for (int i = 0; i < Tokens.Count; i++)
    //    {
    //        if (!Tokens[i].Match(inputTokens[i], args))
    //            return false;
    //    }

    //    return true;
    //}
    public bool TryMatch(string[] inputTokens, out Dictionary<string, object> args)
    {
        args = new Dictionary<string, object>();
        if (Tokens.Count == 0) return false;

        int i = 0;
        for (; i < Tokens.Count; i++)
        {
            if (i >= inputTokens.Length)
                return false;

            if (Tokens[i] is GreedyArgumentToken)
            {
                // consume all remaining tokens
                string rest = string.Join(' ', inputTokens[i..]);
                Tokens[i].Match(rest, args);
                i = inputTokens.Length; // end loop
                break;
            }

            if (!Tokens[i].Match(inputTokens[i], args))
                return false;
        }

        // normal check: allow extra tokens only if last token was greedy
        return i == inputTokens.Length;
    }
}