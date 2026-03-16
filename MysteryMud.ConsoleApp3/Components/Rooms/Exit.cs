using Arch.Core;
using MysteryMud.ConsoleApp3.Enums;

namespace MysteryMud.ConsoleApp3.Components.Rooms;

struct Exit
{
    public string Description;
    public Direction Direction;
    public Entity TargetRoom;
    public bool Closed;
}
