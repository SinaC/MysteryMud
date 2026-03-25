using Arch.Core;
using MysteryMud.Core;
using MysteryMud.GameData.Definitions;
using MysteryMud.Domain.Factories;

namespace MysteryMud.Application.Systems;

public static class SpellSystem
{
    public static SpellDatabase SpellDatabase;

    public static void CastSpell(SystemContext ctx, GameState gameState, Entity caster, Entity target, SpellDefinition spell)
    {
        // TODO: direct damage/heal
        foreach (var effect in spell.Effects)
            EffectFactory.ApplyEffect(ctx, gameState, effect, caster, target);
    }
}