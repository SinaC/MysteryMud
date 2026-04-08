namespace MysteryMud.Core.Effects;

public interface IEffectExecutor
{
    void ResolveDamage(GameState state, DamageAction damageAction);
    void ResolveHeal(GameState state, HealAction healAction);
}
