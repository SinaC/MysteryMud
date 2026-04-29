using DefaultEcs;

namespace MysteryMud.GameData.Intents;

public struct GiveItemIntent
{
    public Entity Entity;
    public Entity Item;
    public Entity Target;
}
