using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Attack;

public struct AttackResult
{
    public EntityId Source;
    public EntityId Target;
    public AttackResultKind Result;
}
