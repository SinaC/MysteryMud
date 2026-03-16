namespace MysteryMud.ConsoleApp3.Commands;

class CommandRegistry
{
    private static readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public static void Register(string name, ICommand cmd)
    {
        _commands[name] = cmd;
    }

    public static bool TryGet(ReadOnlySpan<char> cmdSpan, out ICommand cmd)
    {
        // Convert to string once (low allocation) to query dictionary
        string key = cmdSpan.ToString();
        return _commands.TryGetValue(key, out cmd);
    }
}
