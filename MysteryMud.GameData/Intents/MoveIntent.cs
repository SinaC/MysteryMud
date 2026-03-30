using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct MoveIntent
{
    public Entity Actor;
    public Entity FromRoom;
    public Entity ToRoom;
    public DirectionKind Direction;
    public bool AutoLook;
}
