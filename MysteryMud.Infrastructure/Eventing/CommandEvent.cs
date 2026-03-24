using Arch.Core;

namespace MysteryMud.Infrastructure.Eventing;

public struct CommandEvent
{
    public Entity Player;
    public char[] Buffer;
    public int Length;
}
