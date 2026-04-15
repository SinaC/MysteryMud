using MysteryMud.GameData.Definitions;

namespace MysteryMud.Core.Commands;

public interface IExplicitCommand : ICommand
{
    CommandDefinition Definition { get; }
}
