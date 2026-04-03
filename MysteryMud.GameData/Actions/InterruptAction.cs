using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Actions;

public struct InterruptAction
{
    public Entity Target;
    public InterruptReason Reason;
}
