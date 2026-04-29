using DefaultEcs;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct AttackResolvedEvent
{
    public Entity Source;
    public Entity Target;
    public AttackResultKind Result;
}
