using Arch.Core;
using MysteryMud.GameData.Definitions;

namespace MysteryMud.Core.Commands;

public interface ICommand
{
    CommandDefinition Definition { get; }

    void Execute(SystemContext systemContext, GameState state, Entity actor, ReadOnlySpan<char> cmd, ReadOnlySpan<char> args);
}
