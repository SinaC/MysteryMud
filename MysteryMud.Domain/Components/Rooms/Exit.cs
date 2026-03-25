using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Components.Rooms;

public struct Exit
{
    public string Description;
    public Direction Direction;
    public Entity TargetRoom;
    public bool Closed;
}
