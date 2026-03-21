namespace MysteryMud.ConsoleApp3.Commands.v2;

//public class Syntax
//{
//    public string Pattern { get; }
//    private List<ISyntaxToken> Tokens { get; }

//    public Syntax(string pattern)
//    {
//        Pattern = pattern;
//        Tokens = pattern.Split(' ')
//                        .Select(t =>
//                        {
//                            ISyntaxToken token;
//                            if (t.StartsWith("[") && t.EndsWith("*]"))
//                                token = new GreedyArgumentToken(t[1..^2]);
//                            else if (t.StartsWith("[") && t.EndsWith("]"))
//                                token = new ArgumentToken(t[1..^1]);
//                            else if (t.Equals("silver", StringComparison.OrdinalIgnoreCase) || t.Equals("gold", StringComparison.OrdinalIgnoreCase))
//                                return new LiteralArgumentToken(t, "currency"); // capture as argument
//                            else
//                                token = new WordToken(t);
//                            return token;
//                        })
//                        .ToList();
//    }

//    public bool TryMatch(string[] inputTokens, out Dictionary<string, object> args)
//    {
//        args = new Dictionary<string, object>();
//        if (Tokens.Count == 0) return false;

//        int i = 0;
//        for (; i < Tokens.Count; i++)
//        {
//            if (i >= inputTokens.Length)
//                return false;

//            if (Tokens[i] is GreedyArgumentToken)
//            {
//                // consume all remaining tokens
//                string rest = string.Join(' ', inputTokens[i..]);
//                Tokens[i].Match(rest, args);
//                i = inputTokens.Length; // end loop
//                break;
//            }

//            if (!Tokens[i].Match(inputTokens[i], args))
//                return false;
//        }

//        // normal check: allow extra tokens only if last token was greedy
//        return i == inputTokens.Length;
//    }
//}
public class Syntax
{
    public string Pattern { get; }
    public List<ISyntaxToken> Tokens { get; } = new();

    public Syntax(string pattern)
    {
        Pattern = pattern;
        foreach (var t in pattern.Split(' '))
        {
            if (t.StartsWith("[") && t.EndsWith("*]"))
                Tokens.Add(new GreedyArgumentToken(t[1..^2]));
            else if (t.StartsWith("[") && t.EndsWith("]"))
                Tokens.Add(new ArgumentToken(t[1..^1]));
            else if (t.Equals("silver", StringComparison.OrdinalIgnoreCase) || t.Equals("gold", StringComparison.OrdinalIgnoreCase))
                Tokens.Add(new LiteralArgumentToken(t, "currency"));
            else
                Tokens.Add(new WordToken(t));
        }
    }

    public bool TryMatch(ReadOnlySpan<char> input, Span<Token> inputTokens, int tokenCount, out Dictionary<string, object> args)
    {
        args = new Dictionary<string, object>();
        int i = 0, j = 0;

        for (; i < Tokens.Count; i++, j++)
        {
            if (j >= tokenCount) return false;

            if (Tokens[i] is GreedyArgumentToken)
            {
                var greedySpan = input.Slice(inputTokens[j].Start, input.Length - inputTokens[j].Start);
                Tokens[i].Match(greedySpan, args);
                j = tokenCount; // end parsing
                break;
            }

            var tokenSpan = inputTokens[j].Slice(input);
            if (!Tokens[i].Match(tokenSpan, args)) return false;
        }

        return j == tokenCount;
    }
}