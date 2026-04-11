using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class NotAffectedByRule : IAbilityValidationRule // opposite of AffectedByRule
{
    private readonly ulong _effectTagIndex;
    private readonly AbilityValidationFailBehaviour _failBehaviour;
    private readonly string _failMessageKey;

    public NotAffectedByRule(EffectTagId effectTagId, AbilityValidationFailBehaviour failBehaviour, string failMessageKey)
    {
        _effectTagIndex = 1UL << (int)effectTagId;
        _failBehaviour = failBehaviour;
        _failMessageKey = failMessageKey;
    }

    public AbilityValidationResult Validate(Entity target)
    {
        ref var characterEffects = ref target.Get<CharacterEffects>();
        if ((characterEffects.ActiveTags & _effectTagIndex) != 0)
            return new()
            {
                Success = false,
                FailBehaviour = _failBehaviour,
                FailMessageKey = _failMessageKey
            };

        return new()
        {
            Success = true
        };
    }
}
