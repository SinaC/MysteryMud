using Arch.Core;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Data.Definitions;
using MysteryMud.ConsoleApp3.Factories;

namespace MysteryMud.ConsoleApp3.Systems;

public static class SpellSystem
{
    public static SpellDatabase SpellDatabase;

    public static void CastSpell(GameState gameState, Entity caster, Entity target, SpellDefinition spell)
    {
        // TODO: direct damage/heal
        foreach (var effect in spell.Effects)
            EffectFactory.ApplyEffect(gameState, effect, caster, target);
    }
}