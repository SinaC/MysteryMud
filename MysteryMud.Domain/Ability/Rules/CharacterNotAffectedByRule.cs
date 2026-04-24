using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;
using TinyECS;

namespace MysteryMud.Domain.Ability.Rules;

public class CharacterNotAffectedByRule : AbilityValidationRule // opposite of AffectedByRule
{
    private readonly ulong _effectTagIndex;

    public CharacterNotAffectedByRule(TargetCondition condition, AbilityValidationFailBehaviour failBehaviour, string failMessageKey, CharacterEffectTagId effectTagId)
        : base(condition, failBehaviour, failMessageKey)
    {
        _effectTagIndex = 1UL << (int)effectTagId;
    }

    public override AbilityValidationResult Validate(World world, EntityId _, EntityId target)
    {
        ref var characterEffects = ref world.Get<CharacterEffects>(target);
        if ((characterEffects.Data.ActiveTags & _effectTagIndex) != 0)
            return Fail();

        return Success();
    }
}
