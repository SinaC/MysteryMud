using Arch.Core;
using MysteryMud.Domain.Data.Enums;

namespace MysteryMud.Application.Simulation.Calculators;

public static class DamageCalculator
{
    public static int ModifyDamage(Entity target, int damageAmount, DamageType damageType, Entity source)
    {
        return damageAmount; // TODO: apply damage type modifiers, resistances, vulnerabilities, etc.
    }
}
