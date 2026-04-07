using Arch.Core;
using MysteryMud.Core;
using MysteryMud.Core.Services;
using MysteryMud.Domain.Action.Damage;
using MysteryMud.Domain.Action.Heal;

namespace MysteryMud.Domain.Action.Effect;

public ref struct EffectContext
{
    public Entity? Effect; // for non-instant effect
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

