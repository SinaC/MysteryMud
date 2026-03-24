using System.Reflection;

namespace MysteryMud.Core.Command;

public interface ICommandRegistry
{
    void RegisterCommands(IEnumerable<Assembly> assemblies);
    void RegisterCommand(string name, ICommand cmd);
    bool TryGetCommand(ReadOnlySpan<char> cmdSpan, out ICommand? cmd);
}
