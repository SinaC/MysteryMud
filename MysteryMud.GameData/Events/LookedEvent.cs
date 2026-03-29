using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct LookedEvent
{
    public Entity Viewer;

    public LookTargetKind TargetKind;

    public Entity Entity; // room, character, or item being looked at
}
