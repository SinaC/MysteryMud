using Arch.Core;
using MysteryMud.ConsoleApp3.Data.Enums;

namespace MysteryMud.ConsoleApp3.Simulation.Calculators;

public static class AggroCalculator
{
    public static int CalculateDamageAggro(Entity target, Entity source, int damageAmount, DamageType damageType)
    {
        return damageAmount; // TODO
    }

    public static int CalculateHealAggro(Entity target, Entity source, int healAmount)
    {
        return healAmount / 2; // TODO
    }
}
