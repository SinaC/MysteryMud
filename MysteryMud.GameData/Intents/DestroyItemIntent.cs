using DefaultEcs;

namespace MysteryMud.GameData.Intents;

public struct DestroyItemIntent
{
    public Entity Entity;
    public Entity Item;
}
