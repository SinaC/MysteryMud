using Arch.Core;

namespace MysteryMud.GameData.Intents;

public struct ExecuteAbilityIntent
{
    public Entity Source;
    public List<Entity> Targets;

    public int AbilityId;
}
