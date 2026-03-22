using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Rooms;

namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

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

    public bool TryExecute(World world, Entity actor,  string inputStr)
    {
        Console.WriteLine($"Input: {inputStr}");

        var input = inputStr.AsSpan();

        Span<Token> tokens = stackalloc Token[16];
        int tokenCount = Tokenizer.Tokenize(input, tokens);

        if (tokenCount == 0)
            return false;

        // Extract command prefix
        var cmdToken = tokens[0].Slice(input);

        var candidates = _trie.Search(cmdToken, _root);
        if (candidates == null || candidates.Count == 0)
        {
            Console.WriteLine("no matching command found");
            return false;
        }

        // Remove command token
        var argTokens = tokens.Slice(1, tokenCount - 1);
        //int argCount = tokenCount - 1;

        Command bestCommand = null;
        Syntax bestSyntax = null;
        Dictionary<string, ArgValue> bestArgs = null;
        int bestScore = -1;

        foreach (var cmd in candidates)
        {
            var ctx = new CommandContext // TODO: we can probably reuse the same context object and just update the Command/RawInput for each attempt
            {
                World = world,
                Actor = actor,
                RawInput = inputStr,
                Command = cmd
            };

            foreach (var syntax in cmd.Syntaxes.Select(x => new Syntax(x))) // TODO: precompute Syntax objects when loading commands instead of doing it every time we parse input
            {
                // create resolution context for argument resolution
                var res = new ResolutionContext(world, actor, GetInventoryAndRoom(actor));

                int score = syntax.TryMatch(input, argTokens, ctx, res, out var args);

                if (score < 0)
                    continue;

                if (score > bestScore ||
                    (score == bestScore && cmd.Priority > bestCommand?.Priority))
                {
                    bestScore = score;
                    bestCommand = cmd;
                    bestSyntax = syntax;
                    bestArgs = args;
                }
            }
        }

        if (bestCommand == null)
        {
            Console.WriteLine("no matching syntax found for any candidate command");
            return false;
        }

        //
        var finalCtx = new CommandContext
        {
            World = world,
            Actor = actor,
            RawInput = inputStr,
            Command = bestCommand,
            MatchedSyntax = bestSyntax,
            Arguments = bestArgs
        };

        bestCommand.Execute(finalCtx);
        return true;
    }

    private static List<Entity> GetInventoryAndRoom(Entity actor)
    {
        List<Entity> entities = [];

        // inventory
        ref var inventory = ref actor.TryGetRef<Inventory>(out var hasInventory);
        if (hasInventory)
            entities.AddRange(inventory.Items);

        // room content
        ref var position = ref actor.TryGetRef<Position>(out var hasPosition);
        if (hasPosition)
        {
            ref var roomContents = ref position.Room.TryGetRef<RoomContents>(out var hasRoomContents);
            if (hasRoomContents)
                entities.AddRange(roomContents.Items);
        }

        return entities;
    }
}