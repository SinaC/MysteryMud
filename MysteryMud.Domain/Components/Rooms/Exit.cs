using DefaultEcs;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Components.Rooms;

public struct Exit
{
    public string Description;
    public DirectionKind Direction;
    public Entity TargetRoom;
    public bool Closed;
}
