using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct AttackIntent
{
    public Entity Attacker;
    public Entity Target;
    public int RemainingHits;
    public bool IsReaction; // to prevent infinite loops, reactions can't trigger other reactions
    public bool IgnoreDefense; // e.g., for true skill hits (dont check parry/dodge/...)
    public DamageSourceTypes SourceType;
}
