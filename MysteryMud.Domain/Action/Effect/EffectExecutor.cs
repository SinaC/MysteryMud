using MysteryMud.Core;
using MysteryMud.Core.Effects;
using MysteryMud.Domain.Action.Damage;
using MysteryMud.Domain.Action.Heal;
using MysteryMud.Domain.Action.Move;

namespace MysteryMud.Domain.Action.Effect;

public class EffectExecutor : IEffectExecutor
{
    private readonly IDamageResolver _damageResolver;
    private readonly IHealResolver _healResolver;
    private readonly IMoveResolver _moveResolver;

    public EffectExecutor(IDamageResolver damageResolver, IHealResolver healResolver, IMoveResolver moveResolver)
    {
        _damageResolver = damageResolver;
        _healResolver = healResolver;
        _moveResolver = moveResolver;
    }

    public DamageResult ResolveDamage(GameState state, DamageAction damageAction)
        => _damageResolver.Resolve(state, damageAction);

    public HealResult ResolveHeal(GameState state, HealAction healAction)
        => _healResolver.Resolve(state, healAction);

    public RestoreMoveResult ResolveMove(GameState state, RestoreMoveAction restoreMoveAction)
        => _moveResolver.Resolve(state, restoreMoveAction);
}
