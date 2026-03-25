using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Core.Command;
using MysteryMud.Core.Extensions;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using System.Reflection;

namespace MysteryMud.Infrastructure.Command;

public class CommandRegistry : ICommandRegistry
{
    private ICommand[] _commands = [];

    public void RegisterCommands(IEnumerable<CommandDefinition> definitions, IEnumerable<Assembly> assemblies)
    {
        var list = new List<ICommand>();

        foreach (var def in definitions)
        {
            var typeName = $"{def.Name.FirstCharToUpper()}Command";

            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == typeName && typeof(ICommand).IsAssignableFrom(t));

            if (type == null) continue;

            var cmd = (ICommand)Activator.CreateInstance(type, def)!;

            list.Add(cmd);

            // Aliases become separate entries (important!)
            foreach (var alias in def.Aliases)
            {
                list.Add(new AliasCommand(alias, cmd));
            }
        }

        // Critical: ordering defines behavior
        _commands = list
            .OrderByDescending(c => c.Definition.Priority)
            .ThenBy(c => c.Definition.Name.Length) // ROM-like feel
            .ToArray();
    }

    public CommandFindResult Find(CommandLevel level, Position position, ReadOnlySpan<char> input)
    {
        bool foundPrefix = false;
        bool anyPermission = false;
        bool anyPosition = false;

        foreach (var cmd in _commands)
        {
            var name = cmd.Definition.Name.AsSpan();

            // Prefix check (core of ROM behavior)
            if (!name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                continue;

            foundPrefix = true;

            if (level < cmd.Definition.RequiredLevel)
                continue;

            anyPermission = true;

            if (position < cmd.Definition.MinimumPosition)
                continue;

            anyPosition = true;

            // Exact match always wins immediately
            if (input.Equals(name, StringComparison.OrdinalIgnoreCase))
                return CommandFindResult.Success(cmd);

            if (!cmd.Definition.AllowAbbreviation)
                continue;

            // FIRST valid match wins
            return CommandFindResult.Success(cmd);
        }

        if (!foundPrefix)
            return CommandFindResult.Fail(CommandFindResultType.NotFound);

        if (!anyPermission)
            return CommandFindResult.Fail(CommandFindResultType.NoPermission);

        if (!anyPosition)
            return CommandFindResult.Fail(CommandFindResultType.WrongPosition);

        return CommandFindResult.Fail(CommandFindResultType.NotFound);
    }

    private sealed class AliasCommand : ICommand
    {
        private readonly string _alias;
        private readonly ICommand _inner;
        private readonly CommandDefinition _definition;

        public AliasCommand(string alias, ICommand inner)
        {
            _alias = alias;
            _inner = inner;
            _definition = new CommandDefinition
            {
                Name = _alias,
                Aliases = [],
                RequiredLevel = _inner.Definition.RequiredLevel,
                MinimumPosition = _inner.Definition.MinimumPosition,
                Priority = _inner.Definition.Priority,
                AllowAbbreviation = _inner.Definition.AllowAbbreviation
            };
        }

        public CommandDefinition Definition => _definition;

        public CommandParseOptions ParseOptions => _inner.ParseOptions;

        public void Execute(SystemContext systemContext, GameState gameState, Entity actor, CommandContext ctx) => _inner.Execute(systemContext, gameState, actor, ctx);
    }
}

// Trie-based implementation - not used in production, but kept for reference and testing
//public class CommandRegistry : ICommandRegistry
//{
//    private readonly Node _root = new(); // trie root for command names and aliases

//    public void RegisterCommands(IEnumerable<CommandDefinition> definitions, IEnumerable<Assembly> assemblies)
//    {
//        var commandTypes = assemblies.SelectMany(a => a.GetTypes()).Where(t => typeof(ICommand).IsAssignableFrom(t) && !t.IsAbstract);
//        foreach (var def in definitions)
//        {
//            // Convention: ICommand class is named <Name>Command
//            var typeName = $"{def.Name.FirstCharToUpper()}Command";
//            var type = commandTypes.FirstOrDefault(t => t.Name == typeName && typeof(ICommand).IsAssignableFrom(t));
//            if (type == null) continue;
//            // Instantiate command with definition constructor
//            var cmd = (ICommand)Activator.CreateInstance(type, def)!;
//            // Register main name and aliases Insert(def.Name, cmd);
//            foreach (var alias in def.Aliases) Insert(alias, cmd);
//        }
//    }
//    // Insert main command private
//    void Insert(string key, ICommand command)
//    {
//        var node = _root;
//        key = key.ToLowerInvariant();
//        foreach (var ch in key)
//        {
//            if (!node.Children.TryGetValue(ch, out var next))
//            {
//                next = new Node(); node.Children[ch] = next;
//            }
//            node = next;
//        }
//        node.Commands.Add(command);
//    }
//    // Main resolution method
//    public CommandFindResult Find(CommandLevel level, Position position, ReadOnlySpan<char> cmdSpan)
//    {
//        var node = _root;
//        foreach (var ch in cmdSpan)
//        {
//            if (!node.Children.TryGetValue(ch, out node))
//                return CommandFindResult.Fail(CommandFindResultType.NotFound);
//        }
//        var allCandidates = CollectCommands(node);
//        if (allCandidates.Count == 0)
//            return CommandFindResult.Fail(CommandFindResultType.NotFound);
//        // Step 1: Permission filter
//        var permitted = allCandidates.FindAll(c => level >= c.Definition.RequiredLevel);
//        if (permitted.Count == 0)
//            return CommandFindResult.Fail(CommandFindResultType.NoPermission);
//        // Step 2: Position filter 
//        var usable = permitted.FindAll(c => position >= c.Definition.MinimumPosition);
//        if (usable.Count == 0)
//            return CommandFindResult.Fail(CommandFindResultType.WrongPosition);
//        // Step 3: Exact match always wins
//        var exact = FindExact(usable, cmdSpan);
//        if (exact != null)
//            return CommandFindResult.Success(exact);
//        // Step 4: Remove non-abbreviatable commands
//        usable.RemoveAll(c => !c.Definition.AllowAbbreviation);
//        if (usable.Count == 0)
//            return CommandFindResult.Fail(CommandFindResultType.NotFound);
//        if (usable.Count == 1)
//            return CommandFindResult.Success(usable[0]);
//        // Step 5: Priority resolution
//        ICommand? best = null;
//        bool ambiguous = false;
//        foreach (var cmd in usable)
//        {
//            if (best == null || cmd.Definition.Priority > best.Definition.Priority)
//            {
//                best = cmd; ambiguous = false;
//            }
//            else if (cmd.Definition.Priority == best.Definition.Priority)
//            {
//                ambiguous = true;
//            }
//        }
//        if (best == null || ambiguous)
//            return CommandFindResult.Fail(CommandFindResultType.Ambiguous);
//        return CommandFindResult.Success(best);
//    }
//    private static ICommand? FindExact(List<ICommand> usable, ReadOnlySpan<char> cmdSpan)
//    {
//        for (int i = 0; i < usable.Count; i++)
//        {
//            var c = usable[i];
//            if (MemoryExtensions.Equals(cmdSpan, c.Definition.Name.AsSpan(), StringComparison.OrdinalIgnoreCase))
//                return c;
//        }
//        return null;
//    }
//    private static List<ICommand> CollectCommands(Node node)
//    {
//        var result = new List<ICommand>();
//        var stack = new Stack<Node>();
//        stack.Push(node);
//        while (stack.Count > 0)
//        {
//            var current = stack.Pop();
//            result.AddRange(current.Commands);
//            foreach (var child in current.Children.Values) stack.Push(child);
//        }
//        return result;
//    }
//    private class Node
//    {
//        public Dictionary<char, Node> Children = [];
//        public List<ICommand> Commands = [];
//    }
//}