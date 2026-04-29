using DefaultEcs;

namespace MysteryMud.Core;

public class GameState
{
    public required long CurrentTick { get; init; }
    public required long CurrentTimeMs { get; init; }
}
