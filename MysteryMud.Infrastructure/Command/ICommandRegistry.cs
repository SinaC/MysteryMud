using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using System.Reflection;

namespace MysteryMud.Infrastructure.Command;

public interface ICommandRegistry
{
    void RegisterCommands(IEnumerable<CommandDefinition> commandDefinitions, IEnumerable<Assembly> assemblies);
    CommandFindResult Find(CommandLevel level, Position position, ReadOnlySpan<char> cmdSpan);
}
