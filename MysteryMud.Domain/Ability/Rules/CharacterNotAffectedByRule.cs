using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class CharacterNotAffectedByRule : AbilityValidationRule // opposite of AffectedByRule
{
    private readonly ulong _effectTagIndex;

    public CharacterNotAffectedByRule(TargetCondition condition, AbilityValidationFailBehaviour failBehaviour, string failMessageKey, CharacterEffectTagId effectTagId)
        : base(condition, failBehaviour, failMessageKey)
    {
        _effectTagIndex = 1UL << (int)effectTagId;
    }

    public override AbilityValidationResult Validate(Entity _, Entity target)
    {
        ref var characterEffects = ref target.Get<CharacterEffects>();
        if ((characterEffects.Data.ActiveTags & _effectTagIndex) != 0)
            return Fail();

        return Success();
    }
}
