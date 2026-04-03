using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Attack;

public struct AttackResult
{
    public Entity Source;
    public Entity Target;
    public AttackResultKind Result;
    public AttackKind Kind;
}
