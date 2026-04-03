using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct AttackResolvedEvent
{
    public Entity Source;
    public Entity Target;
    public AttackResultKind Result;
    public DamageSourceKind SourceKind; // Hit, Spell, DoT, etc
}
