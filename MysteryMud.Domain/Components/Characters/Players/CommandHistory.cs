using MysteryMud.Domain.Commands;

namespace MysteryMud.Domain.Components.Characters.Players;

public struct CommandHistory
{
    public CommandHistoryEntry[] Buffer;
    public int Count;
}
