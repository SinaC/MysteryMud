using MysteryMud.GameData.Actions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct AttackIntent
{
    public AttackKind Kind;
    public bool Cancelled;

    public HitAction Hit;
    public AbilityAction Ability;
}
