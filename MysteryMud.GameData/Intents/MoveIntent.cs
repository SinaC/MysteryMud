using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct MoveIntent
{
    public EntityId Actor;
    public EntityId FromRoom;
    public EntityId ToRoom;
    public DirectionKind Direction;
    public bool AutoLook;
}
