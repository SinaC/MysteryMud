using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Combat.Calculators;

public static class DamageCalculator
{
    public static int ModifyDamage(Entity target, int damageAmount, DamageKind damageKind, Entity source)
    {
        return damageAmount; // TODO: apply damage type modifiers, resistances, vulnerabilities, etc.
    }
}
