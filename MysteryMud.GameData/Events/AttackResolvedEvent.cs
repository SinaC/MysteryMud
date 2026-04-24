using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct AttackResolvedEvent
{
    public EntityId Source;
    public EntityId Target;
    public AttackResultKind Result;
}
