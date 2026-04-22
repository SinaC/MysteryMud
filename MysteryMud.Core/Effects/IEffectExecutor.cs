namespace MysteryMud.Core.Effects;

public interface IEffectExecutor
{
    DamageResult ResolveDamage(GameState state, DamageAction damageAction);
    HealResult ResolveHeal(GameState state, HealAction healAction);
    RestoreMoveResult ResolveMove(GameState state, RestoreMoveAction restoreMoveAction);
}
