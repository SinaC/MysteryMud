using Arch.Core;
using MysteryMud.ConsoleApp3.Core;
using MysteryMud.ConsoleApp3.Data.Definitions;
using MysteryMud.ConsoleApp3.Factories;

namespace MysteryMud.ConsoleApp3.Systems;

public static class SpellSystem
{
    public static SpellDatabase SpellDatabase;

    public static void CastSpell(SystemContext systemContext, GameState gameState, Entity caster, Entity target, SpellDefinition spell)
    {
        // TODO: direct damage/heal
        foreach (var effect in spell.Effects)
            EffectFactory.ApplyEffect(systemContext, gameState, effect, caster, target);
    }
}