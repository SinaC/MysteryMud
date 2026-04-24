using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;

public struct LookedEvent
{
    public EntityId Viewer;

    public LookTargetKind TargetKind;

    public EntityId Entity; // room, character, or item being looked at
}
