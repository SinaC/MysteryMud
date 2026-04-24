using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;
public struct ItemGotEvent
{
    public EntityId Entity;
    public EntityId Item;

    public GetSourceKind SourceKind;

    public EntityId Source; // room or container
}
