using DefaultEcs;
using Microsoft.Extensions.Logging;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Extensions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Services;

public class ResistanceService : IResistanceService
{
    private readonly ILogger _logger;

    public ResistanceService(ILogger logger)
    {
        _logger = logger;
    }

    public ResistanceLevels CheckResistance(Entity victim, DamageKind damageKind)
    {
        ref var irv = ref victim.Get<EffectiveIRV>();

        // Generic resistance
        var defaultResistance = ResistanceLevels.Normal;
        if (damageKind == DamageKind.Bash || damageKind == DamageKind.Pierce || damageKind == DamageKind.Slash) // Physical
        {
            if (irv.Immunities.IsSet(DamageKind.Physical))
                defaultResistance = ResistanceLevels.Immune;
            else if (irv.Resistances.IsSet(DamageKind.Physical))
                defaultResistance = ResistanceLevels.Resistant;
            else if (irv.Vulnerabilities.IsSet(DamageKind.Physical))
                defaultResistance = ResistanceLevels.Vulnerable;
        }
        else // Magic
        {
            if (irv.Immunities.IsSet(DamageKind.Magic))
                defaultResistance = ResistanceLevels.Immune;
            else if (irv.Resistances.IsSet(DamageKind.Magic))
                defaultResistance = ResistanceLevels.Resistant;
            else if (irv.Vulnerabilities.IsSet(DamageKind.Magic))
                defaultResistance = ResistanceLevels.Vulnerable;
        }
        // check specific damage
        var damageKindToCheck = DamageKind.None;
        switch (damageKind)
        {
            case DamageKind.None:
                return ResistanceLevels.None; // no Resistance
            case DamageKind.Bash:
            case DamageKind.Pierce:
            case DamageKind.Slash:
                damageKindToCheck = DamageKind.Physical;
                break;
            case DamageKind.Fire:
                damageKindToCheck = DamageKind.Fire;
                break;
            case DamageKind.Cold:
                damageKindToCheck = DamageKind.Cold;
                break;
            case DamageKind.Lightning:
                damageKindToCheck = DamageKind.Lightning;
                break;
            case DamageKind.Acid:
                damageKindToCheck = DamageKind.Acid;
                break;
            case DamageKind.Poison:
                damageKindToCheck = DamageKind.Poison;
                break;
            case DamageKind.Negative:
                damageKindToCheck = DamageKind.Negative;
                break;
            case DamageKind.Holy:
                damageKindToCheck = DamageKind.Holy;
                break;
            case DamageKind.Energy:
                damageKindToCheck = DamageKind.Energy;
                break;
            case DamageKind.Mental:
                damageKindToCheck = DamageKind.Mental;
                break;
            case DamageKind.Disease:
                damageKindToCheck = DamageKind.Disease;
                break;
            case DamageKind.Drowning:
                damageKindToCheck = DamageKind.Drowning;
                break;
            case DamageKind.Light:
                damageKindToCheck = DamageKind.Light;
                break;
            case DamageKind.Other: // no specific IRV
                return defaultResistance;
            case DamageKind.Harm: // no specific IRV
                return defaultResistance;
            case DamageKind.Charm:
                damageKindToCheck = DamageKind.Charm;
                break;
            case DamageKind.Sound:
                damageKindToCheck = DamageKind.Sound;
                break;
            default:
                _logger.LogError("CharacterBase.CheckResistance: Unknown {schoolType} {damageKind}", nameof(DamageKind), damageKind);
                return defaultResistance;
        }
        // if immune to input damage -> immune
        // if resistant to input damage
        //      if default immune -> immune
        //      else -> resistant
        //  if vulnerable to input damage
        //      if default immune -> resistant
        //      if default resistant -> normal
        //      if default vulnerable -> vulnerable
        if (irv.Immunities.IsSet(damageKindToCheck))
            return ResistanceLevels.Immune;
        if (irv.Resistances.IsSet(damageKindToCheck))
        {
            if (defaultResistance == ResistanceLevels.Immune)
                return ResistanceLevels.Immune;
            return ResistanceLevels.Resistant;
        }
        if (irv.Vulnerabilities.IsSet(damageKindToCheck))
        {
            if (defaultResistance == ResistanceLevels.Immune)
                return ResistanceLevels.Resistant;
            else if (defaultResistance == ResistanceLevels.Resistant)
                return ResistanceLevels.Normal;
            else
                return ResistanceLevels.Vulnerable;
        }
        return defaultResistance;
    }
}
