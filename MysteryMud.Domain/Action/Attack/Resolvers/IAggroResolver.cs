using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Attack.Resolvers;

public interface IAggroResolver
{
    void ResolveFromDamage(GameState state, Entity target, Entity source, decimal damageAmount, DamageKind damageKind);
    void ResolveFromHeal(GameState state, Entity target, Entity source, decimal healAmount);
}