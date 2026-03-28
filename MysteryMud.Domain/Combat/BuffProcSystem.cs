namespace MysteryMud.Domain.Combat;

public class BuffProcSystem
{
    //public struct BuffProc
    //{
    //    public SkillEffect Effect;
    //    public Func<DamageEvent, bool> TriggerCondition; // e.g., triggers on being hit
    //    public float Chance;
    //}

    // Buff procs, reflections
    //private void HandleBuffProcs(World world, AttackResolved resolved, CombatContext ctx, Queue<AttackIntent> hitQueue)
    //{
    //    foreach (var buff in world.GetBuffs(resolved.Target))
    //    {
    //        if (buff.Proc != null && buff.Proc.TriggerCondition(resolved.ToDamageEvent()))
    //        {
    //            if (Random.Shared.NextDouble() <= buff.Proc.Chance)
    //            {
    //                // Add reactive effect (damage, heal, debuff, etc.)
    //                ctx.EffectIntents.Add(new EffectIntent
    //                {
    //                    Source = resolved.Target,
    //                    Target = resolved.Source,
    //                    Effect = buff.Proc.Effect
    //                });
    //            }
    //        }
    //    }
    //}
}
