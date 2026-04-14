using MysteryMud.Core;
using MysteryMud.Core.Effects;

namespace MysteryMud.Domain.Action.Attack.Resolvers;

public interface IWeaponProcResolver
{
    void Resolve(GameState state, AttackResult attack, DamageResult damageResult);
}