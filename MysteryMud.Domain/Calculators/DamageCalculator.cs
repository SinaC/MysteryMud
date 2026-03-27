using Arch.Core;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Calculators;

public static class DamageCalculator
{
    public static int ModifyDamage(Entity target, int damageAmount, DamageTypes damageType, Entity source)
    {
        return damageAmount; // TODO: apply damage type modifiers, resistances, vulnerabilities, etc.
    }
}
