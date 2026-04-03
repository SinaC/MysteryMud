using Arch.Core;

namespace MysteryMud.GameData.Actions;

public struct AbilityAction
{
    public Entity Caster;
    public string AbilityId;
    public List<Entity> Targets; // for multi-target
    public bool IsReaction;
}
