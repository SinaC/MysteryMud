using Arch.Core;

namespace MysteryMud.GameData.Actions;

public struct HitAction
{
    public Entity Attacker;
    public Entity Target;
    public int RemainingHits;
    public bool IsReaction; // to prevent infinite loops, reactions can't trigger other reactions
    public bool IgnoreDefense; // e.g., for true skill hits (dont check parry/dodge/...)
}
