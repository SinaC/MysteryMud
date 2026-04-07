using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Combat.Calculators;

public static class DamageCalculator
{
    public static decimal ModifyDamage(Entity target, decimal damageAmount, DamageKind damageKind, Entity source)
    {
        return damageAmount; // TODO: apply damage type modifiers, resistances, vulnerabilities, etc.
    }
}
