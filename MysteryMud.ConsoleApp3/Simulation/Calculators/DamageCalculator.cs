using Arch.Core;
using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Simulation.Calculators;

public static class DamageCalculator
{
    public static int ModifyDamage(Entity target, int damageAmount, DamageType damageType, Entity source)
    {
        return damageAmount; // TODO: apply damage type modifiers, resistances, vulnerabilities, etc.
    }
}
