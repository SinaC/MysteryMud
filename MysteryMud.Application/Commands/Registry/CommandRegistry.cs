namespace MysteryMud.Application.Commands.Registry;

public static class CommandRegistry
{
    // TODO: trie
    private static readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public static void Register(string name, ICommand cmd)
    {
        _commands[name] = cmd;
    }

    public static bool TryGet(ReadOnlySpan<char> cmdSpan, out ICommand? cmd)
    {
        // Convert to string once (low allocation) to query dictionary
        string key = cmdSpan.ToString();
        return _commands.TryGetValue(key, out cmd);
    }

    //private readonly Node _root = new();

    //// Insert main command
    //public void InsertCommand(string name, ICommand command)
    //{
    //    Insert(name, command);
    //}

    //// Insert alias
    //public void InsertAlias(string alias, ICommand command)
    //{
    //    Insert(alias, command);
    //}

    //private void Insert(string key, ICommand command)
    //{
    //    var node = _root;
    //    key = key.ToLowerInvariant();

    //    foreach (var ch in key)
    //    {
    //        if (!node.Children.TryGetValue(ch, out var next))
    //        {
    //            next = new Node();
    //            node.Children[ch] = next;
    //        }

    //        node = next;
    //    }

    //    node.Commands.Add(command);
    //}

    //// 🔍 Main resolution method
    //public CommandResult Find(CommandLevel level, Position position, string input)
    //{
    //    input = input.ToLowerInvariant();

    //    var node = _root;

    //    foreach (var ch in input)
    //    {
    //        if (!node.Children.TryGetValue(ch, out node))
    //            return CommandResult.Fail(CommandResultType.NotFound);
    //    }

    //    var allCandidates = CollectCommands(node);

    //    if (allCandidates.Count == 0)
    //        return CommandResult.Fail(CommandResultType.NotFound);

    //    // Step 1: Permission filter
    //    var permitted = allCandidates.FindAll(c => level >= c.RequiredLevel);

    //    if (permitted.Count == 0)
    //        return CommandResult.Fail(CommandResultType.NoPermission);

    //    // Step 2: Position filter
    //    var usable = permitted.FindAll(c => position >= c.MinimumPosition);

    //    if (usable.Count == 0)
    //        return CommandResult.Fail(CommandResultType.WrongPosition);

    //    // Step 3: Exact match always wins
    //    var exact = usable.Find(c =>
    //        c.Name.Equals(input, StringComparison.OrdinalIgnoreCase));

    //    if (exact != null)
    //        return CommandResult.Success(exact);

    //    // Step 4: Remove non-abbreviatable commands
    //    usable.RemoveAll(c => !c.AllowAbbreviation);

    //    if (usable.Count == 0)
    //        return CommandResult.Fail(CommandResultType.NotFound);

    //    if (usable.Count == 1)
    //        return CommandResult.Success(usable[0]);

    //    // Step 5: Priority resolution
    //    ICommand? best = null;
    //    bool ambiguous = false;

    //    foreach (var cmd in usable)
    //    {
    //        if (best == null || cmd.Priority > best.Priority)
    //        {
    //            best = cmd;
    //            ambiguous = false;
    //        }
    //        else if (cmd.Priority == best.Priority)
    //        {
    //            ambiguous = true;
    //        }
    //    }

    //    if (best == null || ambiguous)
    //        return CommandResult.Fail(CommandResultType.Ambiguous);

    //    return CommandResult.Success(best);
    //}

    //private List<ICommand> CollectCommands(Node node)
    //{
    //    var result = new List<ICommand>();
    //    var stack = new Stack<Node>();
    //    stack.Push(node);

    //    while (stack.Count > 0)
    //    {
    //        var current = stack.Pop();

    //        result.AddRange(current.Commands);

    //        foreach (var child in current.Children.Values)
    //            stack.Push(child);
    //    }

    //    return result;
    //}

    //private class Node
    //{
    //    public Dictionary<char, Node> Children = new();
    //    public List<ICommand> Commands = new();
    //}
}
