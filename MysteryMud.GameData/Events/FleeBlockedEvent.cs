using TinyECS;

namespace MysteryMud.GameData.Events;

public struct FleeBlockedEvent
{
    public EntityId Entity;
    public FleeBlockedReason Reason;
}

public enum FleeBlockedReason
{
    NotInCombat,
    NoExit,
    FailedToFlee,
}
