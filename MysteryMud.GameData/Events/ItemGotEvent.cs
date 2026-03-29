using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Events;
public struct ItemGotEvent
{
    public Entity Entity;
    public Entity Item;

    public GetSourceKind SourceKind;

    public Entity Source; // room or container
}
