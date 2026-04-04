using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Damage;
using MysteryMud.Domain.Heal;

namespace MysteryMud.Domain.Effect;

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

