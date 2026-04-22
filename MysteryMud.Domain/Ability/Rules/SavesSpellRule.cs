using Arch.Core;
using MysteryMud.Core.Random;
using MysteryMud.Domain.Extensions;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class SavesSpellRule : AbilityValidationRule
{
    private readonly IRandom _random;
    private readonly DamageKind _damageKind;

    public SavesSpellRule(IRandom random, TargetCondition condition, AbilityValidationFailBehaviour failBehaviour, string failMessageKey, DamageKind damageKind)
        : base(condition, failBehaviour, failMessageKey)
    {
        _random = random;
        _damageKind = damageKind;
    }

    public override AbilityValidationResult Validate(Entity source, Entity target)
    {
        var save = 50 + (target.Level - source.Level);
        save = Math.Clamp(save, 5, 95);
        if (_random.Chance(save))
            return Fail(); // fail because target successfully saves vs spell
        return Success();
        // TODO
        //var victim = this;
        //var save = 50 + (victim.Level - level) * 5 - victim[CharacterAttributes.SavingThrow] * 2;
        //if (victim.CharacterFlags.IsSet("Berserk"))
        //    save += victim.Level / 2;
        //var resistanceResult = ResistanceCalculator.CheckResistance(victim, damageType);
        //switch (resistanceResult)
        //{
        //    case ResistanceLevels.Immune:
        //        return true;
        //    case ResistanceLevels.Resistant:
        //        save += 2;
        //        break;
        //    case ResistanceLevels.Vulnerable:
        //        save -= 2;
        //        break;
        //}
        //if (victim.Classes.CurrentResourceKinds(victim.Shape).Contains(ResourceKinds.Mana) == true)
        //    save = (save * 9) / 10;
        //save = Math.Clamp(save, 5, 95);
        //return RandomManager.Chance(save);
    }
}
