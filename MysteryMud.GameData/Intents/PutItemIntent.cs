using DefaultEcs;

namespace MysteryMud.GameData.Intents;

public struct PutItemIntent
{
    public Entity Entity;
    public Entity Item;
    public Entity Container;
}
