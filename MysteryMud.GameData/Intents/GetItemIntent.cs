using DefaultEcs;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct GetItemIntent
{
    public Entity Entity;
    public Entity Item;

    public GetSourceKind SourceKind;

    public Entity Source; // room or container
}
