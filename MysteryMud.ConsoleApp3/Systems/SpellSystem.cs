using Arch.Core;
using MysteryMud.ConsoleApp3.Data;
using MysteryMud.ConsoleApp3.Factories;

namespace MysteryMud.ConsoleApp3.Systems;

public static class SpellSystem
{
    public static SpellDatabase SpellDatabase;

    public static void CastSpell(World world, Entity caster, Entity target, SpellDefinition spell)
    {
        // TODO: direct damage/heal
        foreach (var effect in spell.Effects)
            EffectFactory.ApplyEffect(world, effect, caster, target);
    }
}