using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Components.Rooms;

public struct Exit
{
    public string Description;
    public DirectionKind Direction;
    public EntityId TargetRoom;
    public bool Closed;
}
