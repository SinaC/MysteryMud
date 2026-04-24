using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct LookIntent
{
    public EntityId Viewer;

    public LookTargetKind TargetKind;

    public EntityId Target; // room, character, or item being looked at

    public LookMode Mode;
}
