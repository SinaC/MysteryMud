using MysteryMud.GameData.Definitions;

namespace MysteryMud.Core.Commands;

public sealed class RegisteredCommand
{
    public required CommandDefinition Definition { get; init; }
    public required ICommand Handler { get; init; }
}
