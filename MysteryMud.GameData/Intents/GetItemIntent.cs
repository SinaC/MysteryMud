using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct GetItemIntent
{
    public EntityId Entity;
    public EntityId Item;

    public GetSourceKind SourceKind;

    public EntityId Source; // room or container
}
