using DefaultEcs;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct UseAbilityIntent
{
    public int AbilityId;

    public Entity Source;

    public TargetKind TargetKind; // Single, All, Indexed, Self
    public int TargetIndex; // for N.something
    public string TargetName; // empty for 'all' or 'self'

    public bool Cancelled;
}
