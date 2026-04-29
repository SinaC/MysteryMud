using DefaultEcs;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct RoomEnteredEvent
{
    public Entity Entity;
    public Entity FromRoom;
    public Entity ToRoom;
    public DirectionKind? Direction; // can be null in case of teleport
    public bool AutoLook;
}
