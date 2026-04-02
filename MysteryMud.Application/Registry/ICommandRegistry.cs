using MysteryMud.Core.Commands;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using System.Reflection;

namespace MysteryMud.Application.Registry;

public interface ICommandRegistry
{
    void RegisterCommands(IEnumerable<CommandDefinition> commandDefinitions, IEnumerable<Assembly> assemblies, IEnumerable<ICommand> explicitCommands);
    CommandFindResult Find(CommandLevelKind level, PositionKind positionType, ReadOnlySpan<char> cmdSpan, out ICommand? command);
    IEnumerable<CommandDefinition> GetCommandDefinitions(CommandLevelKind commandLevel);
    IEnumerable<CommandDefinition> GetCommandDefinitions<TCommand>()
        where TCommand : ICommand;
    bool TryGetById(int id, out ICommand? command);
}
