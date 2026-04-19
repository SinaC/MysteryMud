using System.Reflection;

namespace MysteryMud.Benchmarks.CommandRegistry;

public class RomCommandRegistry
{
    private ICommand[] _commands = [];

    public void RegisterCommands(IEnumerable<CommandDefinition> definitions, IEnumerable<Assembly> assemblies)
    {
        var list = new List<ICommand>();

        foreach (var def in definitions)
        {
            var cmd = new DummyCommand(def);
            list.Add(cmd);
            foreach (var alias in def.Aliases ?? Array.Empty<string>())
                list.Add(new AliasCommand(alias, cmd));
        }

        // Critical: ordering defines behavior
        _commands = list
            .OrderByDescending(c => c.Definition.Priority)
            .ThenBy(c => c.Definition.Name.Length) // ROM-like feel
            .ToArray();
    }

    public ICommand? Find(CommandLevel level, Position position, ReadOnlySpan<char> input)
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
                return cmd;

            if (cmd.Definition.DisallowAbbreviation)
                continue;

            // FIRST valid match wins
            return cmd;
        }

        if (!foundPrefix)
            return null;

        if (!anyPermission)
            return null;

        if (!anyPosition)
            return null;

        return null;
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
                DisallowAbbreviation = _inner.Definition.DisallowAbbreviation
            };
        }

        public CommandDefinition Definition => _definition;
    }
}
