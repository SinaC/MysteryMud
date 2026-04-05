using Arch.Core;

namespace MysteryMud.GameData.Intents;

public struct UseAbilityIntent
{
    public Entity Source;
    public List<Entity> Targets;

    public int AbilityId;
}
