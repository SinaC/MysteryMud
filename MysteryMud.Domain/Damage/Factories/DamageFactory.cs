using MysteryMud.GameData.Actions;
using MysteryMud.GameData.Enums;
using MysteryMud.GameData.Events;

namespace MysteryMud.Domain.Damage.Factories;

public class DamageFactory
{
    public DamageAction CreateHitDamage(AttackResolved resolved) // no need to check if source/target is alive
    {
        //ref var effectiveStats = ref resolved.Source.Get<EffectiveStats>();
        // TODO: calculate damage based on stats, skills, buffs, etc.
        // TODO: calculate damage type based on weapon

        var damageAction = new DamageAction
        {
            Source = resolved.Source,
            Target = resolved.Target,
            Amount = 5, // TODO: calculate damage based on stats, skills, buffs, etc.
            DamageKind = DamageKind.Physical,
            SourceKind = DamageSourceKind.Hit
        };

        return damageAction;
    }
}
