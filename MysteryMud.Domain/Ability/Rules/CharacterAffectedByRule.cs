using DefaultEcs;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class CharacterAffectedByRule : AbilityValidationRule // opposite of NotAffectedByRule
{
    private readonly ulong _effectTagIndex;

    public CharacterAffectedByRule(TargetCondition condition, AbilityValidationFailBehaviour failBehaviour, string failMessageKey, CharacterEffectTagId effectTagId)
        : base(condition, failBehaviour, failMessageKey)
    {
        _effectTagIndex = 1UL << (int)effectTagId;
    }

    public override AbilityValidationResult Validate(Entity _, Entity target)
    {
        ref var characterEffects = ref target.Get<CharacterEffects>();
        if ((characterEffects.Data.ActiveTags & _effectTagIndex) != _effectTagIndex)
            return Fail();

        return Success();
    }
}
