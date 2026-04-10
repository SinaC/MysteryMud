using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct UseAbilityIntent
{
    public Entity Source;

    public TargetKind TargetKind; // Single, All, Indexed, Self
    public int TargetIndex; // for N.something
    public string TargetName; // empty for 'all' or 'self'

    public AbilityKind AbilityKind;
    public int AbilityId;
}
