using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Combat.Damage;
using MysteryMud.Domain.Combat.Heal;

namespace MysteryMud.Domain.Combat.Effect;

public ref struct EffectContext
{
    public Entity Effect;
    public Entity Source;
    public Entity Target;

    public int IncomingDamage;
    public int LastDamage;

    public int StackCount;

    public GameState State;
    public IGameMessageService Msg;
    public DamageResolver DamageResolver;
    public HealResolver HealResolver;
}

