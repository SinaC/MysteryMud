using Arch.Core;
using Arch.Core.Extensions;
using MysteryMud.Domain.Components.Characters;
using MysteryMud.GameData.Enums;

namespace MysteryMud.Domain.Ability.Rules;

public class AffectedByRule : IAbilityValidationRule // opposite of NotAffectedByRule
{
    private readonly ulong _effectTagIndex;
    private readonly AbilityValidationRuleFailActions _failActions;
    private readonly string _failMessageKey;

    public AffectedByRule(EffectTagId effectTagId, AbilityValidationRuleFailActions failActions, string failMessageKey)
    {
        _effectTagIndex = 1UL << (int)effectTagId;
        _failActions = failActions;
        _failMessageKey = failMessageKey;
    }

    public AbilityValidationResult Validate(Entity target, AbilityRuntime ability)
    {
        ref var characterEffects = ref target.Get<CharacterEffects>();
        if ((characterEffects.ActiveTags & _effectTagIndex) != _effectTagIndex)
            return new()
            {
                Success = false,
                FailActions = _failActions,
                FailureMessageKey = _failMessageKey
            };

        return new()
        {
            Success = true
        };
    }
}
