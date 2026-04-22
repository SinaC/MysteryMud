namespace MysteryMud.Domain.Action.Attack.Resolvers;

public class BuffProcResolver
{
    //public struct BuffProc
    //{
    //    public SkillEffect Effect;
    //    public Func<DamageEvent, bool> TriggerCondition; // e.g., triggers on being hit
    //    public int Chance;
    //}

    // Buff procs, reflections
    //private void HandleBuffProcs(World world, AttackResolved resolved, CombatContext ctx, Queue<AttackIntent> hitQueue)
    //{
    //    foreach (var buff in world.GetBuffs(resolved.Target))
    //    {
    //        if (buff.Proc != null && buff.Proc.TriggerCondition(resolved.ToDamageEvent()))
    //        {
    //            if (_random.Chance(Chance))
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
