using MysteryMud.Core;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Action.Attack.Resolvers;

public interface IAggroResolver
{
    void ResolveFromDamage(GameState state, EntityId target, EntityId source, int damageAmount, DamageKind damageKind);
    void ResolveFromHeal(GameState state, EntityId target, EntityId source, int healAmount);
}