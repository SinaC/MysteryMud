namespace MysteryMud.ConsoleApp3.Commands.ContextBasedParser;

public class CommandTrie
{
    public void AddCommand(Command cmd, CommandTrieNode root)
    {
        var node = root;
        foreach (var c in cmd.Name)
        {
            char lowerC = char.ToLowerInvariant(c);
            if (!node.Children.TryGetValue(lowerC, out var child))
            {
                child = new CommandTrieNode();
                node.Children[lowerC] = child;
            }
            child.Commands.Add(cmd);
            node = child;
        }
    }

    public List<Command> Search(ReadOnlySpan<char> prefix, CommandTrieNode root)
    {
        var node = root;

        foreach (var c in prefix)
        {
            if (!node.Children.TryGetValue(char.ToLowerInvariant(c), out node))
                return null;
        }

        return node.Commands;
    }
}

public class CommandTrieNode
{
    public Dictionary<char, CommandTrieNode> Children = new Dictionary<char, CommandTrieNode>();
    public List<Command> Commands = new();
}
