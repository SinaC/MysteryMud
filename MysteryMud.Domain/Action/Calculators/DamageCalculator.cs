using TinyECS;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Calculators;

public static class DamageCalculator
{
    public static decimal ModifyDamage(EntityId target, EntityId source, decimal damageAmount, DamageKind damageKind)
    {
        return damageAmount; // TODO: apply damage type modifiers, resistances, vulnerabilities, etc.
    }
}
