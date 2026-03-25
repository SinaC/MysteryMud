using System.Reflection;

namespace MysteryMud.Benchmarks.CommandRegistry;

public class OriginalCommandRegistry
{
    private readonly Node _root = new(); // trie root for command names and aliases

    public void RegisterCommands(IEnumerable<CommandDefinition> definitions, IEnumerable<Assembly> assemblies)
    {
        foreach (var def in definitions)
        {
            var cmd = new DummyCommand(def);
            Insert(def.Name, cmd);
            foreach (var alias in def.Aliases)
                Insert(alias, cmd);
        }
    }

    // Insert main command private
    void Insert(string key, ICommand command)
    {
        var node = _root;
        key = key.ToLowerInvariant();
        foreach (var ch in key)
        {
            if (!node.Children.TryGetValue(ch, out var next))
            {
                next = new Node(); node.Children[ch] = next;
            }
            node = next;
        }
        node.Commands.Add(command);
    }

    // Main resolution method
    public ICommand? Find(CommandLevel level, Position position, ReadOnlySpan<char> cmdSpan)
    {
        var node = _root;
        foreach (var ch in cmdSpan)
        {
            if (!node.Children.TryGetValue(ch, out node))
                return null;
        }
        var allCandidates = CollectCommands(node);
        if (allCandidates.Count == 0)
            return null;
        // Step 1: Permission filter
        var permitted = allCandidates.FindAll(c => level >= c.Definition.RequiredLevel);
        if (permitted.Count == 0)
            return null;
        // Step 2: Position filter 
        var usable = permitted.FindAll(c => position >= c.Definition.MinimumPosition);
        if (usable.Count == 0)
            return null;
        // Step 3: Exact match always wins
        var exact = FindExact(usable, cmdSpan);
        if (exact != null)
            return exact;
        // Step 4: Remove non-abbreviatable commands
        usable.RemoveAll(c => !c.Definition.AllowAbbreviation);
        if (usable.Count == 0)
            return null;
        if (usable.Count == 1)
            return usable[0];
        // Step 5: Priority resolution
        ICommand? best = null;
        bool ambiguous = false;
        foreach (var cmd in usable)
        {
            if (best == null || cmd.Definition.Priority > best.Definition.Priority)
            {
                best = cmd; ambiguous = false;
            }
            else if (cmd.Definition.Priority == best.Definition.Priority)
            {
                ambiguous = true;
            }
        }
        if (best == null || ambiguous)
            return null;
        return best;
    }
    private static ICommand? FindExact(List<ICommand> usable, ReadOnlySpan<char> cmdSpan)
    {
        for (int i = 0; i < usable.Count; i++)
        {
            var c = usable[i];
            if (MemoryExtensions.Equals(cmdSpan, c.Definition.Name.AsSpan(), StringComparison.OrdinalIgnoreCase))
                return c;
        }
        return null;
    }
    private static List<ICommand> CollectCommands(Node node)
    {
        var result = new List<ICommand>();
        var stack = new Stack<Node>();
        stack.Push(node);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            result.AddRange(current.Commands);
            foreach (var child in current.Children.Values) stack.Push(child);
        }
        return result;
    }
    private class Node
    {
        public Dictionary<char, Node> Children = [];
        public List<ICommand> Commands = [];
    }
}