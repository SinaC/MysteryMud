using Arch.Core;

namespace MysteryMud.GameData.Intents;

public struct PutItemIntent
{
    public Entity Entity;
    public Entity Item;
    public Entity Container;
}
