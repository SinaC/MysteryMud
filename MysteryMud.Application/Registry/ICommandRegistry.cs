using MysteryMud.Application.ExplicitCommands;
using MysteryMud.Core.Commands;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;
using System.Reflection;

namespace MysteryMud.Application.Registry;

public interface ICommandRegistry
{
    void RegisterCommands(IEnumerable<CommandDefinition> commandDefinitions, IEnumerable<Assembly> assemblies, IEnumerable<IExplicitCommand> explicitCommands);
    CommandFindResult Find(CommandLevelKind level, PositionKind positionType, ReadOnlySpan<char> cmdSpan, out RegisteredCommand? command);
    IEnumerable<RegisteredCommand> GetCommands(CommandLevelKind commandLevel);
    IEnumerable<RegisteredCommand> GetCommands<TCommand>()
        where TCommand : ICommand;
    bool TryGetById(int id, out RegisteredCommand? command);
}
