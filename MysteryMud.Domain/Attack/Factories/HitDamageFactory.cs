using MysteryMud.Domain.Damage;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Attack.Factories;

public class HitDamageFactory
{
    public DamageAction CreateHitDamage(AttackResult attackResult) // no need to check if source/target is alive
    {
        //ref var effectiveStats = ref resolved.Source.Get<EffectiveStats>();
        // TODO: calculate damage based on stats, skills, buffs, etc.
        // TODO: calculate damage type based on weapon

        var damageAction = new DamageAction
        {
            Source = attackResult.Source,
            Target = attackResult.Target,
            Amount = 5, // TODO: calculate damage based on stats, skills, buffs, etc.
            DamageKind = DamageKind.Physical,
            SourceKind = DamageSourceKind.Hit
        };

        return damageAction;
    }
}
