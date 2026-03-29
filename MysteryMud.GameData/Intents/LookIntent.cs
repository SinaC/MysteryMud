using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct LookIntent
{
    public Entity Viewer;

    public LookTargetKind TargetKind;

    public Entity Target; // room, character, or item being looked at

    public LookMode Mode;
}
