using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct RoomEnteredEvent
{
    public EntityId Entity;
    public EntityId FromRoom;
    public EntityId ToRoom;
    public DirectionKind? Direction; // can be null in case of teleport
    public bool AutoLook;
}
