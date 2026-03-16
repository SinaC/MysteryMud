using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Buff;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Enums;

namespace MysteryMud.ConsoleApp3.Systems;

class StatSystem
{
    public static void Recalculate(World world)
    {
        var query = new QueryDescription()
                .WithAll<CharacterStats, EffectiveStats, DirtyStats>()
                .WithNone<DeadTag>();
        world.Query(query, (Entity character,
                     ref CharacterStats stats,
                     ref EffectiveStats eff,
                     ref DirtyStats dirty) =>
        {
            ApplyBaseStats(ref stats, ref eff);
            eff = ApplyBuffModifiers(world, character, ref eff);
            ComputeDerived(ref eff);

            character.Remove<DirtyStats>();
        });
    }

    static void ApplyBaseStats(ref CharacterStats baseStats, ref EffectiveStats eff)
    {
        eff.Strength = baseStats.Strength;
        eff.Dexterity = baseStats.Dexterity;
        eff.Intelligence = baseStats.Intelligence;
        // TODO:...other stats
    }

    static EffectiveStats ApplyBuffModifiers(World world, Entity character, ref EffectiveStats baseStats)
    {
        var effCopy = baseStats;

        // TODO: optimize by only querying buffs that target this character, instead of all buffs in the world
        var query = new QueryDescription()
            .WithAll<BuffTarget, BuffModifiers>();
        world.Query(query, (ref BuffTarget target, ref BuffModifiers mods) => // Lambda only modifies local copy — no ref captured
        {
            if (target.Target != character)
                return;

            foreach (var mod in mods.Values)
                ApplyModifier(ref effCopy, mod);
        });

        return effCopy;
    }

    static void ApplyModifier(ref EffectiveStats eff, StatModifier mod)
    {
        switch (mod.Stat)
        {
            case StatType.Strength:
                eff.Strength = Apply(eff.Strength, mod);
                break;

            case StatType.Dexterity:
                eff.Dexterity = Apply(eff.Dexterity, mod);
                break;

                // TODO: other stats
        }
    }

    static int Apply(int value, StatModifier mod)
    {
        switch (mod.Type)
        {
            case ModifierType.Add:
                return value + mod.Value;

            case ModifierType.Multiply:
                return value * mod.Value;

            case ModifierType.Override:
                return mod.Value;
        }

        return value;
    }

    static void ComputeDerived(ref EffectiveStats eff)
    {
        eff.HitRoll = eff.Dexterity / 2;
        eff.DamRoll = eff.Strength / 2;
        eff.Armor = eff.Dexterity;
    }
}
