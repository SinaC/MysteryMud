using TinyECS;

namespace MysteryMud.Infrastructure.Eventing;

public struct CommandEvent
{
    public EntityId Player;
    public char[] Buffer;
    public int Length;
}
