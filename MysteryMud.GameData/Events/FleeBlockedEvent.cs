using Arch.Core;

namespace MysteryMud.GameData.Events;

public struct FleeBlockedEvent
{
    public Entity Entity;
    public FleeBlockedReason Reason;
}

public enum FleeBlockedReason
{
    NotInCombat,
    NoExit,
    FailedToFlee,
}
