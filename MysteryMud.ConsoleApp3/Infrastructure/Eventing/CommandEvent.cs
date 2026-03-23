using Arch.Core;

namespace MysteryMud.ConsoleApp3.Infrastructure.Eventing;

public struct CommandEvent
{
    public Entity Player;
    public char[] Buffer;
    public int Length;
}
