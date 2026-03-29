namespace MysteryMud.Domain.Combat.Resolvers;

public class WeaponProcResolver
{
    //public struct WeaponProc
    //{
    //    public SkillEffect Effect;   // Could be Damage, Heal, Buff, Debuff, etc.
    //    public float Chance;         // 0..1 probability
    //}

    //private void HandleWeaponProcs(World world, AttackResolved resolved, CombatContext ctx)
    //{
    //    var weapon = world.GetEquippedWeapon(resolved.Source);
    //    foreach (var proc in weapon?.Procs ?? Enumerable.Empty<WeaponProc>())
    //    {
    //        if (Random.Shared.NextDouble() <= proc.Chance)
    //        {
    //            ctx.EffectIntents.Add(new EffectIntent
    //            {
    //                Source = resolved.Source,
    //                Target = resolved.Target,
    //                Effect = proc.Effect
    //            });
    //        }
    //    }
    //}

}
