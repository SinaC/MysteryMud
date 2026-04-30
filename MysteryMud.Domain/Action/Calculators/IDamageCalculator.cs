using DefaultEcs;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Calculators;

public interface IDamageCalculator
{
    decimal ModifyDamage(Entity target, decimal damageAmount, DamageKind damageKind, Entity source);
}