using DefaultEcs;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Action.Calculators;

public class DamageCalculator : IDamageCalculator
{
    private readonly IResistanceService _resistanceService;

    public DamageCalculator(IResistanceService resistanceService)
    {
        _resistanceService = resistanceService;
    }

    public decimal ModifyDamage(Entity target, decimal damageAmount, DamageKind damageKind, Entity source)
    {
        // This is a simple damage curve that reduces the effectiveness of high damage amounts.
        if (damageAmount > 35)
            damageAmount = (damageAmount - 35) / 2 + 35;
        if (damageAmount > 80)
            damageAmount = (damageAmount - 80) / 2 + 80;

        // Check for resistances if the damage amount is significant enough to matter.
        if (damageAmount > 1)
        {
            var resistanceLevel = _resistanceService.CheckResistance(target, damageKind);
            switch (resistanceLevel)
            {
                case ResistanceLevels.Immune:
                    damageAmount = 0;
                    break;
                case ResistanceLevels.Resistant:
                    damageAmount = 2 * damageAmount / 3;
                    break;
                case ResistanceLevels.Vulnerable:
                    damageAmount = 3 * damageAmount / 2;
                    break;
            }
        }

        return damageAmount;
    }
}
