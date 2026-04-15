using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MysteryMud.Core.Commands;
using MysteryMud.Core.Extensions;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using System.Reflection;

namespace MysteryMud.Application.Registry;

public class CommandRegistry : ICommandRegistry
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    private readonly Dictionary<int, RegisteredCommand> _commandById = [];
    private RegisteredCommand[] _commands = [];

    public CommandRegistry(ILogger logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public void RegisterCommands(IEnumerable<CommandDefinition> definitions, IEnumerable<Assembly> assemblies, IEnumerable<IExplicitCommand> explicitCommands)
    {
        var list = new List<RegisteredCommand>();

        // First register explicit commands passed in (e.g. HelpCommand) - these take absolute precedence over convention-based ones
        foreach (var explicitCommand in explicitCommands ?? [])
        {
            // Add command for main name
            list.Add(CreateRegisteredCommand(explicitCommand));

            // Aliases become separate entries (important!)
            foreach (var alias in explicitCommand.Definition.Aliases)
            {
                list.Add(CreateRegisteredAlias(alias, explicitCommand));
            }
        }

        // Then register convention-based commands from assemblies
        var commandTypes = assemblies.SelectMany(a => a.GetTypes()).Where(t => typeof(ICommand).IsAssignableFrom(t) && !t.IsAbstract);
        foreach (var def in definitions)
        {
            var typeName = $"{def.Name.FirstCharToUpper()}Command";
            var type = commandTypes.FirstOrDefault(t => t.Name == typeName && typeof(ICommand).IsAssignableFrom(t));

            if (type == null)
            {
                _logger.LogError("No matching command implementation ({typeName}) found for command definition: {commandDefinitionName}", typeName, def.Name);
                continue;
            }

            // DI resolves the command
            var cmd = (ICommand)_serviceProvider.GetRequiredService(type);

            // Add command for main name
            list.Add(CreateRegisteredCommand(def, cmd));

            // Aliases become separate entries (important!)
            foreach (var alias in def.Aliases)
            {
                list.Add(CreateRegisteredAlias(alias, def, cmd));
            }
        }

        // Critical: ordering defines behavior
        _commands = list
            .OrderByDescending(c => c.Definition.Priority)
            .ThenBy(c => c.Definition.Name.Length) // ROM-like feel
            .ToArray();

        // register command by id
        foreach (var cmd in _commands)
        {
            if (_commandById.ContainsKey(cmd.Definition.Id))
                throw new Exception("Command ID collision");
            _commandById.Add(cmd.Definition.Id, cmd);
        }
    }

    public CommandFindResult Find(CommandLevelKind level, PositionKind positionType, ReadOnlySpan<char> input, out RegisteredCommand? command)
    {
        var foundPrefix = false;
        var anyPermission = false;
        var anyPosition = false;

        command = null;

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

            if (positionType < cmd.Definition.MinimumPosition)
                continue;

            anyPosition = true;

            // Exact match always wins immediately
            if (input.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                command = cmd;
                return CommandFindResult.Success;
            }

            if (cmd.Definition.DisallowAbbreviation)
                continue;

            // FIRST valid match wins
            command = cmd;
            return CommandFindResult.Success;
        }

        if (!foundPrefix)
            return CommandFindResult.NotFound;

        if (!anyPermission)
            return CommandFindResult.NoPermission;

        if (!anyPosition)
            return CommandFindResult.WrongPosition;

        return CommandFindResult.NotFound;
    }

    public IEnumerable<RegisteredCommand> GetCommands(CommandLevelKind commandLevel)
        => _commands.Where(c => c.Definition.RequiredLevel <= commandLevel);

    public IEnumerable<RegisteredCommand> GetCommands<TCommand>()
        where TCommand : ICommand
        => _commands.Where(x => x.Handler is TCommand);

    public bool TryGetById(int id, out RegisteredCommand? command)
        => _commandById.TryGetValue(id, out command);

    private RegisteredCommand CreateRegisteredCommand(CommandDefinition definition, ICommand cmd)
        => new()
        {
            Handler = cmd,
            Definition = definition
        };

    private RegisteredCommand CreateRegisteredCommand(IExplicitCommand cmd)
        => CreateRegisteredCommand(cmd.Definition, cmd);

    private RegisteredCommand CreateRegisteredAlias(string alias, CommandDefinition definition, ICommand cmd)
    {
        var aliasDefinition = new CommandDefinition
        {
            Id = alias.ComputeUniqueId(),
            Name = alias,
            Aliases = [],
            CannotBeForced = definition.CannotBeForced,
            RequiredLevel = definition.RequiredLevel,
            MinimumPosition = definition.MinimumPosition,
            Priority = definition.Priority,
            DisallowAbbreviation = definition.DisallowAbbreviation,
            HelpText = definition.HelpText,
            Syntaxes = definition.Syntaxes,
            Categories = definition.Categories,
            ThrottlingCategories = definition.ThrottlingCategories,
        };
        return new RegisteredCommand
        {
            Handler = cmd,
            Definition = aliasDefinition
        };
    }

    private RegisteredCommand CreateRegisteredAlias(string alias, IExplicitCommand cmd)
        => CreateRegisteredAlias(alias, cmd.Definition, cmd);
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
//        usable.RemoveAll(c => c.Definition.DisallowAbbreviation);
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
