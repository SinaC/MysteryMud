using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.GameData.Intents;

public struct ActionIntent
{
    public ActionKind Kind;

    public AttackData Attack;
    public EffectData Effect;

    public bool Cancelled;
}
