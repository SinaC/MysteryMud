using Arch.Core;
using MysteryMud.ConsoleApp.Components;
using MysteryMud.ConsoleApp.Components.Effects;
using MysteryMud.ConsoleApp.Events;

namespace MysteryMud.ConsoleApp.Systems;

static class CombatEventSystem
{
    private static readonly Random rng = new Random();

    public static void Process(World world, CombatEventQueue queue, CommandBuffer cmd)
    {
        foreach (var evt in queue.DamageEvents)
        {
            int dmg = evt.Amount;

            // 1. Shields absorb damage first
            if (world.Has<Shield>(evt.Target))
            {
                ref var shield = ref world.Get<Shield>(evt.Target);
                int absorbed = Math.Min(shield.AbsorbAmount, dmg);
                dmg -= absorbed;
                shield.AbsorbAmount -= absorbed;
                Console.WriteLine($"{evt.Target.Id} absorbs {absorbed} damage with shield!");

                if (shield.AbsorbAmount <= 0)
                    cmd.Add(w => w.Remove<Shield>(evt.Target));
            }

            // 2. Sanctuary / damage reduction buffs
            if (world.Has<Sanctuary>(evt.Target))
                dmg /= 2;

            // 3. Apply final damage
            if (dmg > 0)
            {
                ref var hp = ref world.Get<Health>(evt.Target);
                hp.Current -= dmg;
                Console.WriteLine($"{evt.Target.Id} takes {dmg} damage from {evt.Source.Id}.");

                if (hp.Current <= 0)
                    cmd.Add(w => w.Add(evt.Target, new DeadTag()));
            }

            // 4. Thorns reactive damage
            if (world.Has<Thorns>(evt.Target))
            {
                int thornDmg = world.Get<Thorns>(evt.Target).Damage;
                cmd.Add(w => w.Get<Health>(evt.Source).Current -= thornDmg);
                Console.WriteLine($"{evt.Source.Id} is hit by thorns for {thornDmg} damage!");
            }

            // 5. Lifesteal / healing procs
            if (world.Has<Lifesteal>(evt.Source))
            {
                int heal = (int)(dmg * world.Get<Lifesteal>(evt.Source).Percent);
                cmd.Add(w => w.Get<Health>(evt.Source).Current += heal);
                Console.WriteLine($"{evt.Source.Id} heals {heal} HP from lifesteal!");
            }

            // 6. Proc effects
            ApplyProcs(world, evt.Source, evt.Target, ref dmg);
        }

        queue.Clear();
    }

    static void ApplyProcs(World world, Entity attacker, Entity target, ref int damage)
    {
        if (!world.Has<Equipment>(attacker))
            return;

        var eq = world.Get<Equipment>(attacker);

        foreach (var item in eq.Slots.Values)
        {
            if (!world.Has<ProcEffect>(item))
                continue;

            var proc = world.Get<ProcEffect>(item);

            if (rng.NextDouble() <= proc.Chance)
            {
                damage += proc.BonusDamage;

                Console.WriteLine("Flames erupt from the weapon!");
            }
        }
    }
}
