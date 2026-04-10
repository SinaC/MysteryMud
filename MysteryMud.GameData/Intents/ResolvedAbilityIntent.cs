using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct ResolvedAbilityIntent
{
    public Entity Source;
    public List<Entity> Targets; // or NativeList / pooled list

    public AbilityKind AbilityKind;
    public int AbilityId;

    public bool Cancelled;

}
