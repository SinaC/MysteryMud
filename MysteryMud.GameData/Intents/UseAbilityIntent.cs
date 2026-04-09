using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct UseAbilityIntent
{
    public Entity Source;
    public List<Entity> Targets;

    public AbilityKind Kind;
    public int AbilityId;

    public bool Cancelled;
}
