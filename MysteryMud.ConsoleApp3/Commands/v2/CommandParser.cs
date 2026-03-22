using Arch.Core;

namespace MysteryMud.ConsoleApp3.Commands.v2;

public class CommandParser
{
    private readonly List<Command> Commands;
    private readonly CommandTrie _trie = new();
    private readonly CommandTrieNode _root = new();

    public CommandParser(IEnumerable<Command> commands)
    {
        Commands = commands.ToList();
        foreach (var command in Commands)
            _trie.AddCommand(command, _root);
    }

    public bool TryExecute(World world, Entity actor, string inputStr)
    {
        var input = inputStr.AsSpan();
        Span<Token> tokens = stackalloc Token[16]; // max 16 tokens, adjust if needed
        int tokenCount = Tokenizer.Tokenize(input, tokens);

        // Extract command prefix
        var cmdToken = tokens[0].Slice(input);

        var candidates = _trie.Search(cmdToken, _root);
        if (candidates == null || candidates.Count == 0)
        {
            Console.WriteLine("no matching command found");
            return false;
        }

        foreach (var cmd in candidates)
        {
            foreach (var syntax in cmd.Syntaxes.Select(s => new Syntax(s)))
            {
                if (syntax.TryMatch(input, tokens, tokenCount, out var args))
                {
                    // CommandContext
                    var ctx = new CommandContext
                    {
                        Command = cmd,
                        CommandName = cmd.Name,
                        Actor = actor,
                        Arguments = args,
                        RawInput = inputStr,
                        MatchedSyntax = syntax.Pattern,
                    };

                    // Execute the command
                    cmd.Execute(ctx);
                    return true;
                }
            }
        }

        // No command matched
        Console.WriteLine("Unknown command: " + inputStr);
        return false;
    }

   
}