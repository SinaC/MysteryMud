using MysteryMud.Core;
using MysteryMud.Core.Effects;
using MysteryMud.Domain.Action.Damage;
using MysteryMud.Domain.Action.Heal;

namespace MysteryMud.ConsoleApp.Hosting;

public class EffectExecutor : IEffectExecutor
{
    private readonly IDamageResolver _damageResolver;
    private readonly IHealResolver _healResolver;

    public EffectExecutor(IDamageResolver damageResolver, IHealResolver healResolver)
    {
        _damageResolver = damageResolver;
        _healResolver = healResolver;
    }

    public DamageResult ResolveDamage(GameState state, DamageAction damageAction)
        => _damageResolver.Resolve(state, damageAction);

    public HealResult ResolveHeal(GameState state, HealAction healAction)
        => _healResolver.Resolve(state, healAction);
}
