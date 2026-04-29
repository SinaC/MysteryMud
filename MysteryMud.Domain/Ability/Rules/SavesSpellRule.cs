using DefaultEcs;
using MysteryMud.Core.Random;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.Domain.Extensions;
using MysteryMud.Domain.Services;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class SavesSpellRule : AbilityValidationRule
{
    private readonly IResistanceService _resistanceService;
    private readonly IRandom _random;
    private readonly DamageKind _damageKind;

    public SavesSpellRule(IResistanceService resistanceService, IRandom random, TargetCondition condition, AbilityValidationFailBehaviour failBehaviour, string failMessageKey, DamageKind damageKind)
        : base(condition, failBehaviour, failMessageKey)
    {
        _resistanceService = resistanceService;
        _random = random;
        _damageKind = damageKind;
    }

    public override AbilityValidationResult Validate(Entity source, Entity target)
    {
        ref var effectiveStats = ref target.Get<EffectiveStats>();

        var save = 50 + (target.Level - source.Level) * 5 - effectiveStats.Values[CharacterStatKind.SavingThrow] * 2;
        var resistanceResult = _resistanceService.CheckResistance(target, _damageKind);
        switch (resistanceResult)
        {
            case ResistanceLevels.Immune:
                return Fail();
            case ResistanceLevels.Resistant:
                save += 2;
                break;
            case ResistanceLevels.Vulnerable:
                save -= 2;
                break;
        }
        save = Math.Clamp(save, 5, 95);
        if (_random.Chance(save))
            return Fail(); // fail because target successfully saves vs spell
        return Success();
    }
}
