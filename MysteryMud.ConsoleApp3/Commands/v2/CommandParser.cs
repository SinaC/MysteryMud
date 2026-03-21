using Arch.Core;
using System.Text.RegularExpressions;

namespace MysteryMud.ConsoleApp3.Commands.v2;

public class CommandParser
{
    private readonly List<Command> Commands;

    public CommandParser(IEnumerable<Command> commands)
    {
        Commands = commands.ToList();
    }

    // Pass in player entity ID
    public bool TryExecute(Entity actor, string input)
    {
        var tokens = Tokenize(input);
        if (tokens.Length == 0) return false;

        string cmdName = tokens[0];
        var command = Commands.FirstOrDefault(c => c.Name.Equals(cmdName, StringComparison.OrdinalIgnoreCase));
        if (command == null) return false;

        foreach (var syn in command.Syntaxes.Select(s => new Syntax(s)))
        {
            if (syn.TryMatch(tokens, out var args))
            {
                var ctx = new CommandContext
                {
                    CommandName = cmdName,
                    RawInput = input,
                    // arguments will be populated by the syntax matcher
                    Actor = actor,
                    MatchedSyntax = syn.Pattern
                };

                foreach (var kv in args)
                    ctx.Arguments[kv.Key] = kv.Value;

                command.Execute(ctx);
                return true;
            }
        }

        Console.WriteLine("invalid command syntax:" + input);
        return false;
    }

    private string[] Tokenize(string input)
    {
        var matches = Regex.Matches(input, @"'([^']*)'|(\S+)");
        return matches.Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value).ToArray();
    }
}