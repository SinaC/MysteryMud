using Arch.Core;

namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

// TODO: replace AllOfToken and AllToken wutg ArgumentToken with special properties (e.g. IsAll, IsAllOf) to simplify matching and resolution logic
public class Syntax
{
    // TODO: store in Command
    public List<ISyntaxToken> Tokens = new();
    public string Pattern { get; }

    public Syntax(string pattern)
    {
        Pattern = pattern;

        if (string.IsNullOrWhiteSpace(pattern))
            return; // no tokens

        var parts = pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var t in parts)
        {
            // --- "all"
            if (t.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                Tokens.Add(new ArgumentToken
                {
                    Name = "all",
                    Kind = ArgKind.Item, // TODO: this is a bit hacky, we should ideally infer the kind from context or have a separate token type for "all"
                    Scope = ArgScope.Room,
                    AllowAll = true
                });
            }
            // --- "all.[room.item]"
            else if (t.StartsWith("all.[") && t.EndsWith("]"))
            {
                var inner = t.Substring(5, t.Length - 6); // room.item

                var arg = ParseArgumentToken(inner);
                arg.AllowAllOf = true;

                Tokens.Add(arg);
            }
            // --- GREEDY ARGUMENT ---
            else if (t.StartsWith("[") && t.EndsWith("*]"))
            {
                var name = t[1..^2]; // strip [ and *]
                Tokens.Add(new GreedyArgumentToken(name));
            }
            // --- NORMAL ARGUMENT ---
            else if (t.StartsWith("[") && t.EndsWith("]"))
            {
                var name = t[1..^1];
                Tokens.Add(ParseArgumentToken(name));
            }
            // --- LITERAL TOKENS ---
            else if (t.Equals("silver", StringComparison.OrdinalIgnoreCase))
            {
                Tokens.Add(new LiteralArgumentToken("silver", "currency", ArgKind.String, ArgValue.String("silver".AsSpan())));
            }
            else if (t.Equals("gold", StringComparison.OrdinalIgnoreCase))
            {
                Tokens.Add(new LiteralArgumentToken("gold", "currency", ArgKind.String, ArgValue.String("gold".AsSpan())));
            }
            // -- - OPTIONAL TOKENS ---
            else if (t.Equals("from", StringComparison.OrdinalIgnoreCase) || t.Equals("to", StringComparison.OrdinalIgnoreCase) || t.Equals("in", StringComparison.OrdinalIgnoreCase))
            {
                Tokens.Add(new OptionalToken(t));
            }
            // --- WORD TOKENS ---
            else
            {
                Tokens.Add(new WordToken(t));
            }
        }
    }

    public static ArgumentToken ParseArgumentToken(string token)
    {
        // token like "[room.item]", "[container.item]", "[item]"
        token = token.Trim('[', ']');

        var parts = token.Split('.', 2);
        var arg = new ArgumentToken();

        if (parts.Length == 2)
        {
            switch (parts[0].ToLower())
            {
                case "room": arg.Scope = ArgScope.Room; break;
                case "inventory": arg.Scope = ArgScope.Inventory; break;
                case "container": arg.Scope = ArgScope.ContainerOnly; break;
                default: arg.Scope = ArgScope.InventoryThenRoom; break;
            }
            arg.Kind = InferArgKind(parts[1]);
            arg.Name = parts[1];
        }
        else
        {
            arg.Kind = InferArgKind(token);
            arg.Name = parts[0];
            arg.Scope = ArgScope.InventoryThenRoom; // default
        }

        return arg;
    }

    private static ArgKind InferArgKind(string name)
    {
        return name.ToLower() switch
        {
            "item" => ArgKind.Item,
            "container" => ArgKind.Container,
            "drinkcontainer" => ArgKind.Item,
            "player" => ArgKind.Player,
            "amount" => ArgKind.Amount,
            // TODO: other heuristics for inferring type? e.g. suffixes like "Id", "Name", etc.
            _ => ArgKind.String
        };
    }

    private struct RawArg
    {
        public int Start;
        public int Length;
        public ISyntaxToken SyntaxToken;

        public ReadOnlySpan<char> Slice(ReadOnlySpan<char> input) => input.Slice(Start, Length);
    }

    // phase 1: match tokens and extract raw argument spans or values
    // phase 2: resolve unresolved arguments, updating context for dependent args (e.g. container arg that item arg depends on) -> don't need resolutionOrder anymore
    
    public int TryMatch(ReadOnlySpan<char> input, Span<Token> inputTokens, CommandContext ctx, ResolutionContext res, out Dictionary<string, ArgValue> resolvedArgs)
    {
        resolvedArgs = new Dictionary<string, ArgValue>();
        var rawArgs = new Dictionary<string, RawArg>();

        int tokenIndex = 0;
        int inputIndex = 0;
        int matchedTokens = 0;

        while (tokenIndex < Tokens.Count)
        {
            var token = Tokens[tokenIndex];

            if (inputIndex >= inputTokens.Length)
                break;

            var inputToken = inputTokens[inputIndex].Slice(input);

            if (token is GreedyArgumentToken greedy)
            {
                // rest of input
                rawArgs[greedy.Name] = new RawArg
                {
                    Start = inputTokens[inputIndex].Start,
                    Length = input.Length - inputTokens[inputIndex].Start,
                    SyntaxToken = token
                };
                inputIndex = inputTokens.Length;
                tokenIndex++;
                matchedTokens++;
                break;
            }
            else if (token is ArgumentToken arg)
            {
                if ((arg.AllowAll || arg.AllowAllOf) && !inputToken.StartsWith("all", StringComparison.OrdinalIgnoreCase))
                {
                    // for "all" tokens, we allow either "all" or a normal argument value
                    // if it starts with "all", treat it as "all", otherwise treat it as normal argument value
                    // this allows commands like "get all" and "get all.sword" to work with the same syntax
                    return -1; // mismatch, expected "all" or argument value
                }

                rawArgs[arg.Name] = new RawArg
                {
                    Start = inputTokens[inputIndex].Start,
                    Length = inputTokens[inputIndex].Length,
                    SyntaxToken = token,
                };
            }
            else if (token is LiteralArgumentToken literal)
            {
                if (!inputToken.Equals(literal._literal.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    return -1; // mismatch
                rawArgs[literal._argName] = new RawArg
                {
                    Start = inputTokens[inputIndex].Start,
                    Length = inputTokens[inputIndex].Length,
                    SyntaxToken = token,
                };
                matchedTokens++;
            }
            else if (token is WordToken word)
            {
                if (!inputToken.Equals(word._word.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    return -1; // mismatch
                matchedTokens++;
                // don't need to add anything to args for word tokens
            }
            else if (token is OptionalToken optional)
            {
                // if matches, consume token, otherwise skip optional without consuming input
                if (inputToken.Equals(optional._word.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    inputIndex++;
                // optional token doesn't have to match, so just continue without incrementing inputIndex
                tokenIndex++;
                continue;
            }
            else
            {
                throw new InvalidOperationException("Unknown token type");
            }

            tokenIndex++;
            inputIndex++;
        }

        if (inputIndex < inputTokens.Length)
        {
            // extra tokens left, so match fails
            return -1;
        }

        // --- Phase 2: Resolve arguments ---
        var resolutionOrder = new List<string>();

        // simple approach: literals and container first
        if (rawArgs.ContainsKey("container"))
            resolutionOrder.Add("container");

        foreach (var key in rawArgs.Keys)
        {
            if (key != "container")
                resolutionOrder.Add(key);
        }

        int resolvedCount = 0;
        foreach (var name in resolutionOrder)
        {
            var token = rawArgs[name].SyntaxToken;

            var raw = rawArgs[name].Slice(input);
            if (token is ArgumentToken argument)
            {
                var resolveResult = ParameterResolver.TryResolve(ctx, res, argument, raw, out var resolved);
                if (resolveResult)
                {
                    resolvedArgs[name] = resolved;
                    res.Args[name] = resolved; // update context for dependent args
                    resolvedCount++;
                }
                else
                {
                    //resolvedArgs[name] = ArgValue.Raw(raw);
                    //res.Args[name] = resolvedArgs[name];
                    return -1; // fail if any argument fails to resolve, we want all-or-nothing for now
                }
            }
            else if (token is LiteralArgumentToken literalArgument)
            {
                resolvedArgs[name] = literalArgument._rawValue;
                res.Args[name] = resolvedArgs[name];
                resolvedCount++;
            }
            else if (token is GreedyArgumentToken greedyArgumentToken)
            {
                resolvedArgs[name] = ArgValue.String(raw);
                res.Args[name] = resolvedArgs[name];
                resolvedCount++;
            }
        }

        // --- Scoring ---
        int score = 0;

        score += matchedTokens * 10;     // matched tokens
        score += resolvedCount * 20;     // successful resolutions
        score += Tokens.Count;           // slight preference for more specific syntaxes
        return score;
    }
}