using Arch.Core;

namespace MysteryMud.ConsoleApp3.Core.Eventing;

public struct CommandEvent
{
    public Entity Player;
    public char[] Buffer;
    public int Length;
}
