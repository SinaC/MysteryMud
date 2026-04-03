using MysteryMud.GameData.Actions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct AttackIntent
{
    public AttackIntentKind Kind;
    public bool Cancelled;

    public HitAction Attack;
    public AbilityAction Ability;
    //TODO: public ChannelingAbilityIntent Channeling;
    //TODO: public InterruptIntent Interrupt;
}
