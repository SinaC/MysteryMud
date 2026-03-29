using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct MoveIntent
{
    public Entity Actor;
    public Entity FromRoom;
    public Entity ToRoom;
    public Directions Direction;
    public bool AutoLook;
}
