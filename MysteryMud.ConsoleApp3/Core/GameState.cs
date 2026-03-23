using Arch.Core;

namespace MysteryMud.ConsoleApp3.Core;

public class GameState
{
    public required World World { get; init; }
    public required long CurrentTick { get; init; }
}
