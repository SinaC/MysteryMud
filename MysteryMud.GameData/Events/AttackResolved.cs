using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct AttackResolved
{
    public Entity Source;
    public Entity Target;
    public AttackResults Result;
    public DamageSourceTypes SourceType; // Hit, Spell, DoT, etc
}
