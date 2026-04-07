using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Attack;

public struct AttackResult
{
    public Entity Source;
    public Entity Target;
    public AttackResultKind Result;
}
