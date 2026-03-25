using System.Reflection;

namespace MysteryMud.Benchmarks.CommandRegistry;

// ------------------------
// Optimized Abbreviation Trie
// ------------------------
public class OptimizedCommandRegistry
{
    private class Node
    {
        public Dictionary<char, Node>? Children;
        public List<ICommand>? Commands;
        public List<ICommand>? AbbreviatableSubtree;

        public Node GetOrCreateChild(char ch)
        {
            Children ??= new Dictionary<char, Node>();
            if (!Children.TryGetValue(ch, out var next))
            {
                next = new Node();
                Children[ch] = next;
            }
            return next;
        }
    }

    private readonly Node _root = new();

    public void RegisterCommands(IEnumerable<CommandDefinition> definitions, IEnumerable<Assembly> assemblies)
    {
        foreach (var def in definitions)
        {
            var cmd = new DummyCommand(def);
            Insert(def.Name, cmd);
            foreach (var alias in def.Aliases ?? Array.Empty<string>())
                Insert(alias, cmd);
        }
    }

    private void Insert(string key, ICommand command)
    {
        var node = _root;
        key = key.ToLowerInvariant();
        foreach (var ch in key)
        {
            node = node.GetOrCreateChild(ch);
            if (command.Definition.AllowAbbreviation)
            {
                node.AbbreviatableSubtree ??= new List<ICommand>();
                if (!node.AbbreviatableSubtree.Contains(command))
                    node.AbbreviatableSubtree.Add(command);
            }
        }

        node.Commands ??= new List<ICommand>();
        node.Commands.Add(command);

        if (command.Definition.AllowAbbreviation)
        {
            node.AbbreviatableSubtree ??= new List<ICommand>();
            if (!node.AbbreviatableSubtree.Contains(command))
                node.AbbreviatableSubtree.Add(command);
        }
    }

    public ICommand? Find(CommandLevel level, Position position, ReadOnlySpan<char> cmdSpan)
    {
        var node = _root;
        foreach (var ch in cmdSpan)
            if (node.Children == null || !node.Children.TryGetValue(ch, out node))
                return null;

        var exactCandidates = node.Commands ?? new List<ICommand>();
        var usableExact = exactCandidates
            .Where(c => level >= c.Definition.RequiredLevel && position >= c.Definition.MinimumPosition)
            .ToList();

        if (usableExact.Count > 0)
            return usableExact.First();

        var abbrevCandidates = node.AbbreviatableSubtree ?? new List<ICommand>();
        var usableAbbrev = abbrevCandidates
            .Where(c => level >= c.Definition.RequiredLevel && position >= c.Definition.MinimumPosition)
            .ToList();

        return usableAbbrev.FirstOrDefault();
    }
}
