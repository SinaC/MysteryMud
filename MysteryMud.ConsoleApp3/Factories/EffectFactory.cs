using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.ConsoleApp3.Components.Characters;
using MysteryMud.ConsoleApp3.Components.Effects;
using MysteryMud.ConsoleApp3.Data;
using MysteryMud.ConsoleApp3.Systems;

namespace MysteryMud.ConsoleApp3.Factories;

public static class EffectFactory
{
    public static Entity ApplySpell(World world, SpellDefinition spell, Entity caster, Entity target)
    {
        var effect = world.Create(
            new Effect
            {
                Source = caster,
                Target = target
            },
            new Duration
            {
                RemainingTicks = spell.Duration,
                WearOffMessage = spell.WearOffMessage,
            },
            new EffectTag
            {
                Id = spell.Id
            });
        foreach (var template in spell.EffectTemplates)
            template.Apply(world, effect);

        ref var characterEffects = ref world.Get<CharacterEffects>(target);
        characterEffects.Effects.Add(effect);

        target.Add<DirtyStats>();

        // TODO: in room ?
        MessageSystem.Send(target, spell.ApplyMessage);

        return effect;
    }
}
