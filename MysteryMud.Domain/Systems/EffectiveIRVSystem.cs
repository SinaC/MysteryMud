using DefaultEcs;
using MysteryMud.Core;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Components.Effects;
using MysteryMud.Domain.Helpers;
using MysteryMud.GameData.Definitions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Systems;

public class EffectiveIRVSystem
{
    private readonly EntitySet _hasDirtyIRVsEntitySet;

    public EffectiveIRVSystem(World world)
    {
        _hasDirtyIRVsEntitySet = world
            .GetEntities()
            .With<BaseIRV>()
            .With<EffectiveIRV>()
            .With<DirtyIRV>()
            .Without<DeadTag>()
            .AsSet();
    }

    public void Tick(GameState state)
    {
        foreach (var character in _hasDirtyIRVsEntitySet.GetEntities())
        {
            ref var baseIRV = ref character.Get<BaseIRV>();
            ref var effectiveIRV = ref character.Get<EffectiveIRV>();

            ref var characterEffects = ref character.Get<CharacterEffects>();

            var (orImmunities, norImmunities, overridingImmunities) = FlagModifierPipeline.CalculateModifiers<CharacterIRVModifiers, CharacterIRVModifier>(
                characterEffects,
                x => true,
                x => x.Values.Where(x => x.Location == IRVLocation.Immunities),
                x => x.Modifier,
                x => x.DamageKinds);
            var (orResistances, norResistances, overridingResistances) = FlagModifierPipeline.CalculateModifiers<CharacterIRVModifiers, CharacterIRVModifier>(
                characterEffects,
                x => true,
                x => x.Values.Where(x => x.Location == IRVLocation.Resistances),
                x => x.Modifier,
                x => x.DamageKinds);
            var (orVulnerabilities, norVulnerabilities, overridingVulnerabilities) = FlagModifierPipeline.CalculateModifiers<CharacterIRVModifiers, CharacterIRVModifier>(
                characterEffects,
                x => true,
                x => x.Values.Where(x => x.Location == IRVLocation.Vulnerabilities),
                x => x.Modifier,
                x => x.DamageKinds);

            effectiveIRV.Immunities = overridingImmunities ?? ((baseIRV.Immunities | orImmunities) & ~norImmunities);
            effectiveIRV.Resistances = overridingResistances ?? ((baseIRV.Resistances | orResistances) & ~norResistances);
            effectiveIRV.Vulnerabilities = overridingVulnerabilities ?? ((baseIRV.Vulnerabilities | orVulnerabilities) & ~norVulnerabilities);
            character.Remove<DirtyIRV>();
        }
    }
}
