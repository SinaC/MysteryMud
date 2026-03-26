using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using System.Reflection;

namespace MysteryMud.Core.Command;

public interface ICommandRegistry
{
    void RegisterCommands(IEnumerable<CommandDefinition> commandDefinitions, IEnumerable<Assembly> assemblies, params ICommand[] explicitCommands);
    CommandFindResult Find(CommandLevels level, Positions positionType, ReadOnlySpan<char> cmdSpan, out ICommand? command);
    IEnumerable<CommandDefinition> GetCommandDefinitions(CommandLevels commandLevel);
}
