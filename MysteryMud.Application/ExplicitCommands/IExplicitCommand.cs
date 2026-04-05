using MysteryMud.Core.Commands;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Application.ExplicitCommands;

public interface IExplicitCommand : ICommand
{
    CommandDefinition Definition { get; }
}
