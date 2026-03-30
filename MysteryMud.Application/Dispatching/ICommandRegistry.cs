using MysteryMud.Application.Commands;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using System.Reflection;

namespace MysteryMud.Application.Dispatching;

public interface ICommandRegistry
{
    void RegisterCommands(IEnumerable<CommandDefinition> commandDefinitions, IEnumerable<Assembly> assemblies, IEnumerable<ICommand> explicitCommands);
    CommandFindResult Find(CommandLevels level, Positions positionType, ReadOnlySpan<char> cmdSpan, out ICommand? command);
    IEnumerable<CommandDefinition> GetCommandDefinitions(CommandLevels commandLevel);
    IEnumerable<CommandDefinition> GetCommandDefinitions<TCommand>()
        where TCommand : ICommand;
}
